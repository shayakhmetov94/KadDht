using kademlia_dht.Base.Message;
using kademlia_dht.Base.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using System.Diagnostics;

namespace kademlia_dht.Base
{
    /// <summary>
    /// TODO: last seen node after comm
    /// </summary>
    public class KadHashTable
    {
        private Registry _dhtSchedulesRegistry;

        /// <summary>
        /// Owner node
        /// </summary>
        public KadNode Owner { get; private set; }

        /// <summary>
        /// System-wide replication parameter
        /// </summary>
        public int ReplicationCount { get; private set; } = 20;

        /// <summary>
        /// System-wide concurrency parameter
        /// </summary>
        public int MaxConcurrentThreads { get; private set; } = 3;

        /// <summary>
        /// Replication period
        /// </summary>
        public int ReplicationInSecs { get; private set; } = 3600;

        /// <summary>
        /// Republication period
        /// </summary>
        public int RepublicationInSecs { get; private set; } = 86400;

        /// <summary>
        /// Bucket refresh period
        /// </summary>
        public int BucketsRefreshInSecs { get; private set; } = 3600;

        public KadHashTable(KadNode node, KadContactNode knownNode = null, KadHashTableConfiguration kadHashTableConfig = null) {
            Owner = node;
            if(knownNode != null)
                Owner.BucketList.Put( knownNode );

             if(kadHashTableConfig != null) {
                ReplicationCount = kadHashTableConfig.ReplicationCount ?? ReplicationCount;
                MaxConcurrentThreads = kadHashTableConfig.MaxConcurrentThreads ?? MaxConcurrentThreads;
                ReplicationInSecs = kadHashTableConfig.ReplicationInSecs ?? ReplicationInSecs;
                RepublicationInSecs = kadHashTableConfig.RepublicationInSecs ?? RepublicationInSecs;
                BucketsRefreshInSecs = kadHashTableConfig.BucketsRefreshInSecs ?? BucketsRefreshInSecs;
             }
            
            InitTable();
        }

        private void InitTable() {
            RefreshBucket( Owner.Id );

            _dhtSchedulesRegistry = new Registry();
            
            _dhtSchedulesRegistry.Schedule((Action)ReplicateAllValues).ToRunEvery(ReplicationInSecs).Seconds();
            _dhtSchedulesRegistry.Schedule((Action)RepublishAllValues).ToRunEvery(RepublicationInSecs).Seconds();
            _dhtSchedulesRegistry.Schedule((Action)RefreshUnaccessedBuckets).ToRunEvery(BucketsRefreshInSecs).Seconds();

            JobManager.Initialize(_dhtSchedulesRegistry);
            JobManager.Start();
        }

        public void RefreshUnaccessedBuckets() {
            SortedSet<Buckets.Bucket> sortedBuckets = new SortedSet<Buckets.Bucket>(Owner.BucketList.Buckets, new Buckets.Bucket.ByLastUpdatedComparer());
            foreach(var bucket in sortedBuckets) {
                if((DateTime.UtcNow - bucket.LastUpdated).TotalSeconds > BucketsRefreshInSecs) {
                    byte[] intBytes = BitConverter.GetBytes(bucket.Id);
                    byte[] idBytes = new byte[ReplicationCount];
                    Buffer.BlockCopy(intBytes, 0, idBytes, 0, intBytes.Length);
                    KadId bucketId = new KadId(idBytes);
                    RefreshBucket(bucketId);
                } else {
                    break;
                }
            }
        }

        public void RefreshBucket(KadId id) {
            var closestNodes = LookupNode(id);

            foreach ( var contactNode in closestNodes ) {
                switch ( Owner.BucketList.Put( contactNode ) ) {
                    case Buckets.BucketList.BucketPutResult.BucketIsFull: 
                        TryReplaceLeastSeenContactFromBucket( contactNode );
                        break;
                    case Buckets.BucketList.BucketPutResult.Updated:
                    case Buckets.BucketList.BucketPutResult.Success:
                        //Nothing to do here.
                        break;
                }
            }
        }

        public bool TryReplaceLeastSeenContactFromBucket(KadContactNode newContact) {
            var bucket = Owner.BucketList.GetBucket( newContact.Id);
            var leastSeen = bucket.GetLeastSeen();

            NodeMessage pongMsg = Owner.Ping( leastSeen );

            if ( pongMsg == null ) {
                bucket.Replace( leastSeen.Id, newContact );
                return true;
            }

            return false;
        }

        public void StoreValue(KadValue value) {
            if ( !Owner.Storage.Contains( value.Id ) ) {
                Owner.Storage.PutOwnerVal( value );
            }

            ReplicateValue(value);
        }

        public KadValue FindValue(KadId valueId) {
            if ( Owner.Storage.Contains( valueId ) ) {
                var storageVal = Owner.Storage.Get(valueId);
                if ( Owner.IsValueExpired( storageVal ) )
                    Owner.Storage.Remove( valueId );
                else
                    return storageVal;
            }

            SortedSet<KadContactNode> shortList;
            Tuple<KadContactNode,KadValue> value = LookupValue(valueId, out shortList);
            if ( value != null ) {
                if ( shortList.Min.Id != value.Item1.Id )
                    Owner.StoreValue(shortList.Min, value.Item2 );

                Owner.Storage.Put( value.Item2 );
            }

            return value.Item2;
        }

        private void ReplicateAllValues() {
            foreach(KadValue value in Owner.Storage.Values) 
                ReplicateValue( value );
        }

        private void ReplicateValue(KadValue value) {
            var closestNodes = Owner.BucketList.GetClosestNodes(value.Id, ReplicationCount);

            foreach ( var node in closestNodes )
                Owner.StoreValue( node, value ); 
        }

        private void RepublishAllValues() {
            foreach(KadValue value in Owner.Storage.OwnerValues) {
                value.UpdateTimestamp();
                ReplicateValue(value);
            }
        }

        private Tuple<KadContactNode, KadValue> LookupValue( KadId valueId, out SortedSet<KadContactNode> retSortList ) {
            var closestNodes = Owner.BucketList.GetClosestNodes(valueId, MaxConcurrentThreads);
            SortedSet<KadContactNode> shortList = new SortedSet<KadContactNode>(closestNodes, new KadId.KadIdToBaseComparator(valueId));
            retSortList = shortList;
            if ( shortList.Count == 0 ) 
                return null;

            SortedSet<KadId> queriedNodes = new SortedSet<KadId>(new KadId.KadIdToIdComparator());
            KadId closestId = shortList.Min.Id;
            SemaphoreSlim tasksSemaphore = new SemaphoreSlim(MaxConcurrentThreads);
            KadContactNode returnedNode = null;
            KadValue value = null;
            //Should do fine for small alpha 
            List<Task> tasks = new List<Task>();
            while ( queriedNodes.Count < ReplicationCount ) {
                var shortListSnapshot = shortList.Where( ( n ) => !queriedNodes.Contains( n.Id ) ).ToList();
                foreach ( var node in shortListSnapshot ) {
                    tasks.Add( Task.Run( () => {
                        var response = Owner.FindValue(node, valueId);
                        if ( response != null ) {
                            if ( response.Type == MessageType.FindNode ) {
                                foreach ( var contact in response.Contacts ) {
                                    shortList.Add( contact );
                                }
                            } else if ( response.Type == MessageType.FindValue ) {
                                value = new KadValue( valueId, DateTime.Now, response.Payload );
                                returnedNode = node;
                            }
                        }
                        tasksSemaphore.Release();
                    } ) );
                    queriedNodes.Add( node.Id );
                    tasksSemaphore.Wait();
                }

                Task.WaitAll( tasks.ToArray() );
                tasks.Clear();

                if ( (shortList.Min.Id ^ closestId) >= closestId )
                    break;

                closestId = shortList.Min.Id;
            }

            return value != null ? new Tuple<KadContactNode, KadValue>(returnedNode, value) : null;
        }

        private IEnumerable<KadContactNode> LookupNode( KadId nodeId ) {
            var closestNodes = Owner.BucketList.GetClosestNodes(nodeId, MaxConcurrentThreads);
            SortedSet<KadContactNode> shortList = new SortedSet<KadContactNode>(closestNodes, new KadId.KadIdToBaseComparator(nodeId));

            if ( shortList.Count == 0 )
                return shortList;

            SortedSet<KadId> queriedNodes = new SortedSet<KadId>(new KadId.KadIdToIdComparator());
            KadId closestId = shortList.Min.Id;

            SemaphoreSlim tasksSemaphore = new SemaphoreSlim(MaxConcurrentThreads);

            List<Task> tasks = new List<Task>();
            while ( queriedNodes.Count < ReplicationCount ) {
                var shortListSnapshot = shortList.Where( ( n ) => !queriedNodes.Contains( n.Id ) ).ToList();
                foreach ( var node in shortListSnapshot ) {
                    tasks.Add( Task.Run( () => {
                        var contacts = Owner.FindNode(node, nodeId);
                        foreach ( var contact in contacts ) {
                            shortList.Add( contact );
                        }

                        tasksSemaphore.Release();
                    } ) );

                    tasksSemaphore.Wait();
                }

                Task.WaitAll( tasks.ToArray() );
                tasks.Clear();

                if ( (shortList.Min.Id ^ closestId) >= closestId )
                    break;

                closestId = shortList.Min.Id;

            }

            return shortList.Take(ReplicationCount).ToList();
        }

        public void Shutdown() {
            JobManager.StopAndBlock();
            Owner.Shutdown();
        }
    }
}
