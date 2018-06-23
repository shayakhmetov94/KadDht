using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Buckets
{
    public class BucketList
    {
        public enum BucketPutResult { Success, Updated, BucketIsFull }

        private KadId currentNodeId;
        private int k;
        private ConcurrentDictionary<int, Bucket> _buckets;
        private ConcurrentDictionary<int, DateTime> _bucket2RefreshDate;

        public IEnumerable<Bucket> Buckets {
            get {
                return _buckets.Values.ToList();
            }
        }

        public BucketList(KadId nodeId, int nodeCountInBucket) {
            currentNodeId = nodeId;
            k = nodeCountInBucket;
            _buckets = new ConcurrentDictionary<int, Bucket>();
            _bucket2RefreshDate = new ConcurrentDictionary<int, DateTime>();
        }

        public BucketPutResult Put(KadContactNode node) {
            var bucket = GetBucket(node.Id);

            if ( bucket.IsFull() )
                return BucketPutResult.BucketIsFull;

            if ( bucket.Contains( node.Id ) ) {
                bucket.SeenNow( node );
                return BucketPutResult.Updated;
            }

            bucket.Put( node );

            return BucketPutResult.Success;
        }

        public Bucket GetBucket(KadId forId) {
            int bucketId = GetBucketIdForKadId(forId);

            if ( _buckets.ContainsKey( bucketId ) ) 
                return _buckets[bucketId];

            return CreateNewBucket( bucketId );
        }

        public IEnumerable<KadContactNode> GetClosestNodes(KadId closeTo, int count) {
            int bucketId = GetBucketIdForKadId(closeTo);
            var orderedKeys = _buckets.Keys.OrderBy( ( t ) => Math.Abs(t - bucketId)).ToList();
            List<KadContactNode> closests = new List<KadContactNode>();

            foreach (var bucketKey in orderedKeys ) {
                foreach ( var node in _buckets[bucketKey].GetNodes( count - closests.Count ) )
                    closests.Add( node );

                if ( closests.Count == count )
                    break;
            }

            return closests;
        }

        private Bucket CreateNewBucket(int idx) {
            var bucket = new Bucket(idx, k);
            _buckets.AddOrUpdate( idx, bucket, ( k, v ) => v );
            return bucket;
        }

        private int GetBucketIdForKadId(KadId id) {
            BigInteger distance = (id ^ currentNodeId).GetNumericValue();
            return (int)BigInteger.Log(distance, 2);
        }
    }
}
