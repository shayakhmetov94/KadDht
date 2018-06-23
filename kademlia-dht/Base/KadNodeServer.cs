using kademlia_dht.Base.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kademlia_dht.Base
{
    class KadNodeServer
    {
        private object __seqLock = new object();
        private ushort _currentSeq;
        private UdpClient _udpClient;
        private ConcurrentDictionary<ulong, NodeMessage> _messages;
        private bool _stopped;
        
        private Thread _listenerThread;

        public IPEndPoint EndPoint { get; private set; }

        public KadNodeServer(IPEndPoint endPoint) {
            _currentSeq = 1;
            EndPoint = endPoint;
            _messages = new ConcurrentDictionary<ulong, NodeMessage>();
            _stopped = false;
        }

        //TODO: try something else 
        private ushort GenerateSeq() {
            lock ( __seqLock ) {
                if ( _currentSeq == ushort.MaxValue )
                    _currentSeq = 1;

                _currentSeq++;
                
                return _currentSeq;
            }
        }

        private ulong GetMessageId(IPAddress address, ushort seq) {
            ulong messageId = 0;
            byte[] addressBytes = address.GetAddressBytes();
            for ( int i = 8; i < 36; i += 8 ) {
                messageId |= addressBytes[(i / 8) - 1];
                messageId = messageId << i;
            }

            messageId = messageId << 16;
            messageId |= seq;

            return messageId;
        }

        public void Start(KadNode ownerNode) {
            _udpClient = new UdpClient( EndPoint );
            _listenerThread = new Thread(ListenIncomingObj);
            _listenerThread.Start( ownerNode );
        }

        public void Stop(bool forced) {
            if(forced)
                _udpClient.Close();

            _stopped = true;
        }

        public NodeMessage SendMessageSync(IPEndPoint toEndPoint, NodeMessage msg, int timeout) {
            if ( msg.IsRequest )
                msg.Seq = GenerateSeq();

            ManualResetEventSlim eventReset = new ManualResetEventSlim();
            msg.AddCallback( ( m ) =>  eventReset.Set() );
            ulong msgId = GetMessageId( toEndPoint.Address, msg.Seq );
            _messages.AddOrUpdate( msgId, msg, ( k, v ) => v );

            byte[] rawMsg = msg.ToBytes();
            _udpClient.Send( rawMsg, rawMsg.Length, toEndPoint );
            if ( eventReset.Wait( timeout ) ) {
                NodeMessage msgFromQueue;
                if ( _messages.TryRemove( msgId, out msgFromQueue ) )
                    return msgFromQueue;
            }

            return msg;
        }

        public void SendMessage( IPEndPoint toEndPoint, NodeMessage msg) {
            if ( msg.IsRequest )
                throw new InvalidOperationException( "Message can't be request" );

            byte[] rawMsg = msg.ToBytes();
            _udpClient.Send( rawMsg, rawMsg.Length, toEndPoint );
        }

        public void ListenIncomingObj(object ownerNodeObj) {
            ListenIncoming( (KadNode)ownerNodeObj );
        }

        private void ListenIncoming(KadNode ownerNode) {
            IPEndPoint incomingIpEndPoint = new IPEndPoint(IPAddress.Any, EndPoint.Port);
            while ( !_stopped ) {
                try {
                    byte[] rawMsg = _udpClient.Receive(ref incomingIpEndPoint);
                    NodeMessage incomingMsg = new NodeMessage.Builder(rawMsg).Build(); //TODO: msg validation
                    ulong msgIdx = GetMessageId(incomingIpEndPoint.Address, incomingMsg.Seq); //TODO: two spare bytes for port
                    if ( _messages.ContainsKey( msgIdx ) ) {
                        NodeMessage rqMsg = null;
                        if ( _messages.TryRemove( msgIdx, out rqMsg ) )
                            rqMsg.ProcessResponse( incomingMsg );
                    } else {
                        if ( incomingMsg.IsRequest ) {
                            ownerNode.ProcessRequest( incomingMsg, incomingIpEndPoint );
                            _messages.AddOrUpdate( msgIdx, incomingMsg, ( k, v ) => v );
                        }
                    }
                } catch(SocketException se) {
                    if ( se.SocketErrorCode != SocketError.Interrupted )
                        throw se;
                }

                incomingIpEndPoint = new IPEndPoint( IPAddress.Any, EndPoint.Port );
            }
        }
    }
}
