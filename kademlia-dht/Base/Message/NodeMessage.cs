using kademlia_dht.Base.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Message
{
    public enum MessageType { Ping = 0x0, FindNode = 0x1, FindValue = 0x2, CanStoreValue = 0x3, StoreValue = 0x4  }
    public delegate void NodeMessageResponseHandler(NodeMessage response);

    public partial class NodeMessage
    {
        public MessageType Type { get; private set; }
        public ushort Seq { get; set; } = 0;
        public KadId OriginatorId { get; private set; }
        public bool IsRequest { get; private set; }
        public byte[] Payload { get; private set; } = new byte[0];
        public NodeMessage Response { get; private set; }

        //private SortedSet<ContactNode> _contacts;
        public IEnumerable<KadContactNode> Contacts { get { return ReadContactNodes(); } }

        public KadValue Value { get { return ReadKadValue(); } }

        private event NodeMessageResponseHandler OnResponse;

        public void AddCallback( NodeMessageResponseHandler handler ) {
            if(handler != null)
                OnResponse += handler;
        }

        public void ProcessResponse(NodeMessage response) {
            Response = response;
            OnResponse( response );
        }

        public byte[] ToBytes() { 
            MemoryStream ms = new MemoryStream(23 + Payload.Length);
            ms.WriteByte( (byte)Type );
            WriteUShort( ms, Seq );
            ms.Write( OriginatorId.Value, 0, OriginatorId.Value.Length );
            ms.WriteByte( (byte)(IsRequest ? 0 : 1) );
            WriteUShort( ms, (ushort)Payload.Length );
            ms.Write( Payload, 0, Payload.Length );
            return ms.ToArray();
        }

        private static void WriteUShort( MemoryStream ms, ushort val ) {
            var bytes = BitConverter.GetBytes( val );
            Array.Reverse( bytes );
            ms.Write( bytes, 0, 2 );
        }

        private IEnumerable<KadContactNode> ReadContactNodes() {
            int offset = 0;
            byte[] ipv4AddrBytes = new byte[4];
            while ( offset != Payload.Length) {
                KadId contactId = ReadId(Payload, ref offset, 20);
                Buffer.BlockCopy( Payload, offset, ipv4AddrBytes, 0, 4 );
                IPAddress address = new IPAddress(ipv4AddrBytes);
                offset += ipv4AddrBytes.Length;

                int port = BitConverter.ToInt32(Payload, offset);
                offset += 4;

                yield return new KadContactNode( contactId, new IPEndPoint(new IPAddress( ipv4AddrBytes ), port ));
            }
        }

        private KadValue ReadKadValue() {
            int offset = 0;
            KadId valId = ReadId(Payload, ref offset, 20);
            DateTime timestamp = ReadDateTime(Payload, ref offset);

            byte[] value = new byte[Payload.Length - offset];
            Buffer.BlockCopy( Payload, offset, value, 0, value.Length );

            return new KadValue( valId, timestamp, value );
        }

        private KadId ReadId(byte[] buf, ref int offset, int len) {
            byte[] idBytes = new byte[len];
            Buffer.BlockCopy( buf, offset, idBytes, 0, len );
            if (!BitConverter.IsLittleEndian )
                Array.Reverse( idBytes );

            offset += len;
            return new KadId( idBytes );
        }

        private DateTime ReadDateTime( byte[] buf, ref int offset ) {
            return DateTime.FromBinary( ReadLong( buf, ref offset, sizeof(long)) );
        }

        private long ReadLong( byte[] buf, ref int offset, int len ) {
            byte[] lbytes = new byte[len];
            Buffer.BlockCopy( buf, offset, lbytes, 0, len );
            if ( !BitConverter.IsLittleEndian )
                Array.Reverse( lbytes );
            offset += len;
            return BitConverter.ToInt64( lbytes, 0 );
        }

    }
}
