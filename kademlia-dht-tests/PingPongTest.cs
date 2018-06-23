using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using kademlia_dht.Base;
using System.Net;

namespace kademlia_dht_tests
{
    [TestClass]
    public class PingPongTest
    {
        [TestMethod]
        public void PingPong() {
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            KadContactNode contact1 = new KadContactNode(node2.Id, new IPEndPoint(IPAddress.Loopback, 55556));
            node1.BucketList.Put( contact1 );
            node1.Ping( contact1 );

            node1.Shutdown();
            node2.Shutdown();
        }
    }
}
