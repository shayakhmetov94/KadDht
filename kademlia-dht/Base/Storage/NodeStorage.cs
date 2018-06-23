using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Storage
{
    public abstract class NodeStorage
    {
        public abstract IEnumerable<KadValue> Values { get; }
        public abstract IEnumerable<KadValue> OwnerValues { get; }

        public abstract int Size();
        public abstract bool Contains( KadId id );
        public abstract bool IsFull();
        public abstract KadValue Get( KadId id );
        public abstract void Put( KadValue value );
        public abstract bool Remove(KadId id);
        public abstract bool IsOwnerVal(KadId valId);
        public abstract void PutOwnerVal( KadValue value );
    }
}
