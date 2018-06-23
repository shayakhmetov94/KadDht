using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base
{
    public class KadContactNode
    {
        public KadId Id { get; }
        public IPEndPoint EndPoint { get; }

        public KadContactNode(KadId id, IPEndPoint endPoint) {
            Id = id;
            EndPoint = endPoint;
        }

        public override string ToString() {
            return $"Id: {Id.GetNumericValue()} | Address: {EndPoint.Address}:{EndPoint.Port}";
        }

        public class Comparer : IComparer<KadContactNode>
        {
            public int Compare( KadContactNode x, KadContactNode y ) {
                return x.Id.GetNumericValue().CompareTo( y.Id.GetNumericValue() );
            }
        }
    }


}
