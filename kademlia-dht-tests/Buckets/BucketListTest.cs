using System;
using System.Net;
using System.Threading;
using kademlia_dht.Base;
using kademlia_dht.Base.Buckets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kademlia_dht_tests.Buckets
{
    [TestClass]
    public class BucketListTest
    {
        [TestMethod]
        public void BucketList_Put() {
            BucketList bucketList = new BucketList(KadId.GenerateRandom(), 20);

            byte[] kadId = new byte[20];

            for(int i = 0; i < 20; i++) {
                kadId[19] = (byte)(i + 1);
                var contact = new kademlia_dht.Base.KadContactNode(new KadId(kadId), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
                bucketList.Put(contact);
                Thread.Sleep(1000);
            }
            kadId[19] = 20;
            Assert.IsTrue(bucketList.Put(new KadContactNode(new KadId(kadId), new IPEndPoint(IPAddress.Loopback, 20000))) == BucketList.BucketPutResult.BucketIsFull);
        }
    }
}
