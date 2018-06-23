using System;
using System.Collections.Generic;
using System.Linq;
using kademlia_dht.Base.Buckets;
using System.Net;
using kademlia_dht.Base.Message;
using kademlia_dht.Base.Storage;
using System.Diagnostics;

namespace kademlia_dht.Base
{
    public class KadNode {
        private KadNodeServer _server;
        public BucketList BucketList { get; private set; }
        public KadId Id { get; }
        public NodeStorage Storage { get; private set; }
        public IPEndPoint EndPoint {
            get {
                return _server.EndPoint;
            }
        }

        public int TimeoutInMSec { get; set; } = 2000;
        public int ValueExpirationInSec { get; private set; } = 86400;

        public KadNode( IPEndPoint ipEndpoint ) {
            Id = KadId.GenerateRandom();
            InitNode( ipEndpoint );
        }

        public KadNode( IPEndPoint ipEndpoint, KadId id ) {
            Id = id;
            InitNode( ipEndpoint );
        }

        private void InitNode( IPEndPoint ipEndpoint ) {
            BucketList = new BucketList( Id, 20 );
            Storage = new MemoryNodeStorage( 20 );
            _server = new KadNodeServer( ipEndpoint );
            _server.Start( this );
        }

        #region RPC API

        /// <summary>
        /// Pings other node 
        /// </summary>
        /// <param name="id">Node id to ping</param>
        /// <returns></returns>
        public NodeMessage Ping( KadId id ) {
            var bucket = BucketList.GetBucket(id);
            return Ping( bucket.GetNode( id ) );
        }

        /// <summary>
        /// Pings other node
        /// </summary>
        /// <param name="node">Node to ping</param>
        /// <returns></returns>
        public NodeMessage Ping( KadContactNode node ) {
            NodeMessage msg = new NodeMessage.Builder()
                              .SetType(MessageType.Ping)
                              .SetOriginator(node.Id)
                              .Build();
            msg.AddCallback( UpdateLastSeen );
            return _server.SendMessageSync( node.EndPoint, msg, TimeoutInMSec ).Response;
        }

        /// <summary>
        /// Find contact's closest nodes to id
        /// </summary>
        /// <param name="node">Node to query</param>
        /// <param name="id">Id to compare</param>
        /// <returns>Closest nodes to id</returns>
        public IEnumerable<KadContactNode> FindNode( KadContactNode node, KadId id ) {
            NodeMessage msg = new NodeMessage.Builder()
                              .SetType(MessageType.FindNode)
                              .SetOriginator(Id)
                              .SetIsRequest( true )
                              .SetPayload(id.Value)
                              .Build();

            NodeMessage response = _server.SendMessageSync( node.EndPoint, msg, TimeoutInMSec).Response;
            msg.AddCallback( UpdateLastSeen );
            if ( response != null )
                return new List<KadContactNode>( response.Contacts );

            return new List<KadContactNode>();
        }

        /// <summary>
        /// Find contact's closest nodes to id
        /// </summary>
        /// <param name="node">Node to query</param>
        /// <param name="id">Id to compare</param>
        /// <returns>Closest nodes to id</returns>
        public IEnumerable<KadContactNode> FindNode( KadId id, KadId idToFind ) {
            var bucket = BucketList.GetBucket(id);
            return FindNode( bucket.GetNode( id ), idToFind );
        }

        /// <summary>
        /// Store value in contact
        /// </summary>
        /// <param name="node">Contact node</param>
        /// <param name="value">Value to store</param>
        /// <returns>true, if node stored value</returns>
        public bool StoreValue(KadId id, KadValue value) {
            var bucket = BucketList.GetBucket(id);
            return StoreValue(bucket.GetNode(id), value);
        }

        /// <summary>
        /// Store value in contact
        /// </summary>
        /// <param name="node">Contact node</param>
        /// <param name="value">Value to store</param>
        /// <returns>true, if node stored value</returns>
        public bool StoreValue( KadContactNode node, KadValue value ) { //TODO: support chunking
            NodeMessage storeValueMsg = new NodeMessage.Builder()
                                       .SetType(MessageType.StoreValue)
                                       .SetOriginator(Id)
                                       .SetIsRequest(true)
                                       .SetKadValueAsPayload(value)
                                       .Build();
            storeValueMsg.AddCallback( UpdateLastSeen );
            var response = _server.SendMessageSync( node.EndPoint, storeValueMsg, TimeoutInMSec ).Response;

            return response != null && response.Type == MessageType.CanStoreValue && response.Payload[0] > 0;
        }

        /// <summary>
        /// Checks whether value is expired
        /// Algorithm described in http://xlattice.sourceforge.net/components/protocol/kademlia/specs.html#expiration
        /// </summary>
        /// <param name="kadVal">Value to be checked</param>
        /// <returns>True if value expired</returns>
        public bool IsValueExpired( KadValue kadVal ) {
            var valBucket = BucketList.GetBucket(kadVal.Id);
            int cA = 0, cB = 0;
            foreach ( var bucket in BucketList.Buckets.Where( ( b ) => b.Id < valBucket.Id ) ) {
                cA += bucket.NodesCount;
            }

            foreach ( var contact in valBucket.GetNodes() ) {
                if ( contact.Id < kadVal.Id ) {
                    cB++;
                }
            }

            int c = cA + cB;

            if(c == 0) {
                return true;
            }

            int secondsBtw = (int)(DateTime.UtcNow - kadVal.Timestamp).TotalSeconds;
          
            if ( c > Storage.Size() ) {
                if (secondsBtw >= ValueExpirationInSec ) {
                    return true;
                }
            } else if (secondsBtw >= ValueExpirationInSec * Math.Exp( Storage.Size() / c ) ) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if other node can store value
        /// </summary>
        /// <param name="node">Node to check</param>
        /// <returns>True if node can store value</returns>
        public bool CanStoreValue( KadContactNode node ) {
            NodeMessage canStoreValueMsg = new NodeMessage.Builder()
                                           .SetType(MessageType.CanStoreValue)
                                           .SetOriginator(Id)
                                           .SetIsRequest(true)
                                           .Build();

            canStoreValueMsg.AddCallback( UpdateLastSeen );
            NodeMessage response = _server.SendMessageSync( node.EndPoint, canStoreValueMsg, TimeoutInMSec ).Response;

            return response != null && response.Payload[0] > 0;
        }

        /// <summary>
        /// Retrives value from other node
        /// </summary>
        /// <param name="node">Node to query</param>
        /// <param name="valId">Value id</param>
        /// <returns>Value if queried node has value, otherwise closest nodes to id</returns>
        public NodeMessage FindValue(KadId id, KadId valId) {
            var bucket = BucketList.GetBucket(id);
            return FindValue(bucket.GetNode(id), valId);
        }


        /// <summary>
        /// Retrives value from other node
        /// </summary>
        /// <param name="node">Node to query</param>
        /// <param name="valId">Value id</param>
        /// <returns>Value if queried node has value, otherwise closest nodes to id</returns>
        public NodeMessage FindValue( KadContactNode node, KadId valId ) {
            NodeMessage findValMsg = new NodeMessage.Builder()
                                    .SetType(MessageType.FindValue)
                                    .SetOriginator(Id)
                                    .SetIsRequest(true)
                                    .SetPayload(valId.Value)
                                    .Build();
            findValMsg.AddCallback( UpdateLastSeen );

            return _server.SendMessageSync( node.EndPoint, findValMsg, TimeoutInMSec ).Response;
        }

        /// <summary>
        /// Shutdown node
        /// </summary>
        public void Shutdown() {
            _server.Stop( true );
        }

        private void UpdateLastSeen( NodeMessage msg ) {
            var bucket = BucketList.GetBucket( msg.OriginatorId );
            if ( bucket.Contains( msg.OriginatorId ) ) {
                bucket.SeenNow( bucket.GetNode( msg.OriginatorId ) );
            }
        }

        #endregion

        /// <summary>
        /// Processes request from other node. Used by NodeServer class
        /// </summary>
        /// <param name="msg">Request to process</param>
        /// <param name="origAddress">Request originator adress</param>
        public void ProcessRequest( NodeMessage msg, IPEndPoint origAddress ) {
            switch ( msg.Type ) {
                case MessageType.Ping: { //TODO: handle cases when bucket is full, etc
                        NodeMessage respMsg = new NodeMessage.Builder()
                                          .SetType(MessageType.Ping)
                                          .SetOriginator(Id)
                                          .SetSeq(msg.Seq)
                                          .SetIsRequest(false)
                                          .Build();

                        _server.SendMessage( origAddress, respMsg );
                        break;
                    }


                case MessageType.FindNode: {
                        var closestNodes = BucketList.GetClosestNodes(new KadId(msg.Payload), 20);
                        NodeMessage respMsg = new NodeMessage.Builder()
                                             .SetType(MessageType.FindNode)
                                             .SetSeq(msg.Seq)
                                             .SetOriginator( Id )
                                             .SetContacts(closestNodes)
                                             .Build();

                        _server.SendMessage( origAddress, respMsg );
                        break;
                    }

                case MessageType.FindValue: {
                        KadId valueId = new KadId(msg.Payload);

                        NodeMessage.Builder msgBuilder = new NodeMessage.Builder()
                                                        .SetType(MessageType.FindValue)
                                                        .SetSeq(msg.Seq)
                                                        .SetOriginator(Id);

                        if ( Storage.Contains( valueId ) ) {
                            msgBuilder.SetKadValueAsPayload( Storage.Get( valueId ) );
                        } else {
                            var closestNodes = BucketList.GetClosestNodes(valueId, 20);
                            msgBuilder.SetContacts( closestNodes )
                                      .SetType( MessageType.FindNode );
                        }

                        _server.SendMessage( origAddress, msgBuilder.Build() );
                        break;
                    }
                case MessageType.CanStoreValue: {
                        NodeMessage respMsg = new NodeMessage.Builder()
                                              .SetType( MessageType.CanStoreValue )
                                              .SetSeq( msg.Seq )
                                              .SetOriginator( Id )
                                              .SetIsRequest( false )
                                              .SetFlagAsPayload( !Storage.IsFull() )
                                              .Build();

                        _server.SendMessage( origAddress, respMsg );
                        break;
                    }

                case MessageType.StoreValue: {
                        NodeMessage.Builder msgBuilder = new NodeMessage.Builder()
                                                         .SetSeq(msg.Seq)
                                                         .SetOriginator(Id)
                                                         .SetIsRequest(false)
                                                         .SetType( MessageType.CanStoreValue );

                        if ( Storage.IsFull() ) {
                            msgBuilder.SetFlagAsPayload( false );
                        } else {
                            msgBuilder.SetFlagAsPayload( true );
                            Storage.Put( msg.Value );
                        }

                        _server.SendMessage( origAddress, msgBuilder.Build() );
                        break;
                    }

            }

            UpdateLastSeen( msg );
        }
    }

    
}
