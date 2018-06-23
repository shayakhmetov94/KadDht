using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using kademlia_dht.Base;
using kademlia_dht.Base.Message;

namespace kademlia_dht_tests
{
    [TestClass]
    public class NodeMessageParseTest
    {
        [TestMethod]
        public void MessageParseTest() {
            KadId id = KadId.GenerateRandom();
            NodeMessage msg = new NodeMessage.Builder()
                                             .SetType(MessageType.Ping)
                                             .SetOriginator(id)
                                             .Build();

            NodeMessage msgParsed = new NodeMessage.Builder(msg.ToBytes()).Build();

            Assert.AreEqual( id.GetNumericValue(), new KadId( id.Value ).GetNumericValue() );

            Assert.AreEqual( msg.Type, msgParsed.Type );
            Assert.AreEqual( msg.Seq, msgParsed.Seq );
            Assert.AreEqual( msg.OriginatorId.GetNumericValue(), msgParsed.OriginatorId.GetNumericValue() );
        }
    }
}
