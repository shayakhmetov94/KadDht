using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Storage
{
    public class KadValue
    {
        public KadId Id { get; private set; }
        public DateTime Timestamp { get; private set; }
        public byte[] Value { get; private set; }

        public KadValue(KadId id, DateTime timestamp, byte[] value) {
            Id = id;
            Timestamp = timestamp;
            byte[] valueCopy = new byte[value.Length];
            Buffer.BlockCopy(value, 0, valueCopy, 0, value.Length);
            Value = valueCopy;
        }

        /// <summary>
        /// Sets timestamp to DateTime.UtcNow
        /// </summary>
        public void UpdateTimestamp() {
            Timestamp = DateTime.UtcNow;
        }

        public static byte[] ToBytes(KadValue value) {
            MemoryStream memStream = new MemoryStream(value.Id.Value.Length + value.Value.Length);
            memStream.Write( value.Id.Value, 0, value.Id.Value.Length );
            memStream.Write( value.Value, 0, value.Value.Length );

            return memStream.ToArray();
        }

        public static KadValue FromBytes( byte[] value, int idLength ) {
            if ( value == null )
                throw new ArgumentNullException( "value" );

            if ( value.Length <= idLength )
                throw new ArgumentOutOfRangeException( "value", value.Length, "Expected value.length > idLength" );

            byte[] idBytes = new byte[idLength];
            Buffer.BlockCopy( value, 0, idBytes, 0, idLength );
            byte[] valueBytes = new byte[value.Length-idLength];
            Buffer.BlockCopy( value, idLength, valueBytes, 0, valueBytes.Length );

            return new KadValue( new KadId( idBytes ), DateTime.Now, valueBytes );
        }
    }
}
