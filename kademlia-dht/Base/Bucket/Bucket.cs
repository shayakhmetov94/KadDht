using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Buckets
{
    public class Bucket
    {
        private int _size;
        private SortedSet<BucketContactNode> _sortedNodes;
        private List<BucketContactNode> __sortedNodes;

        private Dictionary<KadId, BucketContactNode> _nodesMap;
        private object __rwlock = new object();

        public int Id { get; }
        public int NodesCount { get { return _sortedNodes.Count; } }
        public DateTime LastUpdated { get; private set; }

        public Bucket(int id, int size) {
            Id = id;
            _size = size;
            _sortedNodes = new SortedSet<BucketContactNode>( new BucketContactNode.BucketContactComparer() );
            _nodesMap = new Dictionary<KadId, BucketContactNode>( new KadId.KadIdEqComparer() );
            __sortedNodes = new List<BucketContactNode>();
            LastUpdated = DateTime.UtcNow;
        }

        public void Put(KadContactNode node) {
            lock(__rwlock) {
                if(IsFull())
                    throw new Exception("Bucket is full");

                var newNode = new BucketContactNode(node) { LastUsed = DateTime.UtcNow };
                LastUpdated = DateTime.UtcNow;

                _sortedNodes.Add(newNode);
                _nodesMap.Add(node.Id, newNode);
            }
        }

        public bool IsFull() {
            return _size <= _sortedNodes.Count;
        }

        public bool Contains(KadId id) {
            lock ( __rwlock ) {
                return _nodesMap.ContainsKey( id );
            }
        }

        public KadContactNode GetLeastSeen() {
            lock ( __rwlock ) {
                return _sortedNodes.Min.Node;
            }
        } 

        public void Replace(KadId oldId, KadContactNode newNode) {
            var added = new BucketContactNode(newNode) { LastUsed = DateTime.UtcNow};
            lock ( __rwlock ) {
                if ( !_nodesMap.ContainsKey( oldId ) )
                    throw new Exception( $"Node {oldId} not found" );

                var replaced = _nodesMap[oldId];
                _sortedNodes.Remove( replaced );
                _nodesMap.Remove( oldId );
                
                _sortedNodes.Add( added );
                _nodesMap[added.Node.Id] = added;
            }
        }

        public void SeenNow( KadContactNode node ) {
            Replace( node.Id, node );
        }

        public KadContactNode GetNode(KadId id) {
            return _nodesMap[id].Node;
        }
        public IEnumerable<KadContactNode> GetNodes( ) {
            return GetNodes( NodesCount );
        }

        public IEnumerable<KadContactNode> GetNodes( int count ) {
            return _sortedNodes.Select( ( bn ) => bn.Node ).ToList();
        }

        class BucketContactNode
        {
            public KadContactNode Node { get; }
            public DateTime LastUsed { get; set; }

            public BucketContactNode(KadContactNode node) {
                Node = node;
            }

            public class BucketContactComparer : IComparer<BucketContactNode>, IEqualityComparer<BucketContactNode>
            {
                public int Compare( BucketContactNode x, BucketContactNode y ) {
                    if(x.Node.Id.GetNumericValue() == y.Node.Id.GetNumericValue())
                        return 0;

                    if(x.LastUsed.Equals(y.LastUsed))
                        return -1;

                    return x.LastUsed.CompareTo( y.LastUsed);
                }

                public bool Equals( BucketContactNode x, BucketContactNode y ) {
                    return x.Node.Id.GetNumericValue().Equals( y.Node.Id.GetNumericValue() );
                }

                public int GetHashCode( BucketContactNode obj ) {
                    return obj.Node.Id.GetNumericValue().GetHashCode();
                }
            }

        }

        public class ByLastUpdatedComparer : IComparer<Bucket>
        {
            public int Compare( Bucket x, Bucket y ) {
                return x.LastUpdated.CompareTo( y );
            }
        }
    }
}
