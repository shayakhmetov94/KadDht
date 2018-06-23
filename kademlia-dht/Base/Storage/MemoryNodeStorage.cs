using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Storage
{
    class MemoryNodeStorage : NodeStorage
    {
        private int MaxSize;
        private ConcurrentDictionary<KadId, KadValue> _data;
        private SortedSet<KadId> _ownerValIds;

        public override IEnumerable<KadValue> Values {
            get {
                return _data.Values.Where((val) => !_ownerValIds.Contains(val.Id)).ToList();
            }
        }

        public override IEnumerable<KadValue> OwnerValues {
            get {
                lock ( _ownerValIds ) {
                    return _data.Values.Where( ( val ) => _ownerValIds.Contains( val.Id ) ).ToList();
                }
            }
        }

        public MemoryNodeStorage( int size ) {
            _data = new ConcurrentDictionary<KadId, KadValue>(new KadId.KadIdEqComparer());
            _ownerValIds = new SortedSet<KadId>( new KadId.KadIdToIdComparator());
            MaxSize = size;
        }

        public override int Size() {
            return MaxSize;
        }

        public override bool Contains( KadId id ) {
            if ( id == null )
                throw new ArgumentNullException( "id" );

            return _data.ContainsKey( id );
        }

        public override bool IsFull() {
            return _data.Count >= MaxSize; 
        }

        public override void Put( KadValue value ) {
            if ( IsFull() )
                throw new Exception( "Storage is full" );

            if ( value == null )
                throw new ArgumentNullException( "value" );

            if ( Contains( value.Id ) ) 
                throw new Exception( $"Already exists value with a key {value.Id}" );

            _data.AddOrUpdate( value.Id, value, ( k, v ) => v );
        }

        public override KadValue Get( KadId id ) {
            if ( Contains( id ) ) 
                return _data[id];

            throw new Exception( $"No value with id {id}" );
        }

        public override bool Remove( KadId id ) {
            lock ( _ownerValIds ) {
                if ( _ownerValIds.Contains( id ) ) {
                    _ownerValIds.Remove(id);
                }
            }

            KadValue removed;
            return _data.TryRemove( id, out removed );
        }

        public override void PutOwnerVal( KadValue value ) {
            Put(value);

            lock ( _ownerValIds ) {
                _ownerValIds.Add( value.Id);
            }
        }

        public override bool IsOwnerVal( KadId valId ) {
            lock ( _ownerValIds ) {
                return _ownerValIds.Contains( valId );
            }
        }
    }
}
