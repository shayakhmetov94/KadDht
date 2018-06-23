using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace kademlia_dht.Base.Message
{
    public partial class NodeMessage
    {
        public class Builder
        {
            private NodeMessage _nodeMessage = new NodeMessage();

            public Builder() { }

            public Builder( byte[] msg ) {
                Parse( msg );
            }

            public Builder SetType( MessageType type ) {
                _nodeMessage.Type = type;
                return this;
            }

            public Builder SetSeq( ushort seq ) {
                _nodeMessage.Seq = seq;
                return this;
            }

            public Builder SetOriginator( KadId id ) {
                _nodeMessage.OriginatorId = id;
                return this;
            }

            public Builder SetIsRequest( bool isRequest ) {
                _nodeMessage.IsRequest = isRequest;
                return this;
            }

            public Builder SetPayload( byte[] payload ) {
                _nodeMessage.Payload = payload;
                return this;
            }

            public Builder SetContacts( IEnumerable<KadContactNode> nodes ) {
                MemoryStream conNodeBytes = new MemoryStream(nodes.Count());
                foreach ( var node in nodes )
                    WriteContactNode( conNodeBytes, node );

                return SetPayload( conNodeBytes.ToArray() );
            }

            public Builder SetFlagAsPayload( bool flag ) {
                _nodeMessage.Payload = new byte[] { BoolToByte( flag ) };
                return this;
            }

            public Builder SetKadValueAsPayload(Storage.KadValue value) {
                MemoryStream ms = new MemoryStream(value.Id.Value.Length + value.Value.Length + sizeof(long));
                WriteKadValue( ms, value );
                _nodeMessage.Payload = ms.ToArray();
                return this;
            }

            public Builder Parse( byte[] buf ) {
                SetType( (MessageType)buf[0] );
                int offset = 1;
                SetSeq( ReadUShort( buf, ref offset ) );
                byte[] nodeId = new byte[20];
                Buffer.BlockCopy( buf, offset, nodeId, 0, 20 );

                SetOriginator( new KadId( nodeId ) );
                offset += 20;

                SetIsRequest( buf[offset] == 0 );
                offset++;
                ushort len = ReadUShort(buf, ref offset);
                byte[] payload = new byte[buf.Length-offset];

                if ( len > 0 ) {
                    Buffer.BlockCopy( buf, offset, payload, 0, len );
                    SetPayload( payload );
                }

                return this;
            }

            public NodeMessage Build() {
                return _nodeMessage;
            }

            private static byte BoolToByte( bool val ) {
                byte boolByte = BitConverter.GetBytes(val)[0];
                return BitConverter.IsLittleEndian ? boolByte : (byte)(7 >> boolByte);
            }

            private static ushort ReadUShort( byte[] buf, ref int start ) {
                ushort val = (ushort)((buf[start] << 8) | buf[start + 1]);
                start += 2;
                return val;
            }

            private static void WriteContactNode( MemoryStream ms, KadContactNode contact ) {
                ms.Write( contact.Id.Value, 0, contact.Id.Value.Length );
                byte[] ipBytes = contact.EndPoint.Address.GetAddressBytes();
                ms.Write( ipBytes, 0, ipBytes.Length );
                byte[] portBytes = BitConverter.GetBytes(contact.EndPoint.Port);
                ms.Write( portBytes, 0, portBytes.Length );
            }

            private static void WriteKadValue( MemoryStream ms, Storage.KadValue val ) {
                ms.Write( val.Id.Value, 0, val.Id.Value.Length );
                WriteLong( ms, val.Timestamp.ToBinary() );
                ms.Write( val.Value, 0, val.Value.Length );
            }

            private static void WriteLong( MemoryStream ms, long val ) {
                byte[] lbytes = BitConverter.GetBytes(val);
                if ( !BitConverter.IsLittleEndian )
                    Array.Reverse( lbytes );

                ms.Write( lbytes, 0, lbytes.Length );
            }
        }
    }
}
