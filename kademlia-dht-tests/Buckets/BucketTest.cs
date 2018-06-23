using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using kademlia_dht.Base;
using kademlia_dht.Base.Buckets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kademlia_dht_tests.Buckets
{
    [TestClass]
    public class BucketTest
    {
        [TestMethod]
        [ExpectedException(typeof(Exception), "Bucket is full")]
        public void Bucket_OnFull() {
            Bucket bucket = new Bucket(1, 20);

            for(int i = 0; i < 21; i++) {
                bucket.Put(new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000)));
                Thread.Sleep(1000);
            }
        }

        [TestMethod]
        public void Bucket_PutAndRetrive() {
            Bucket bucket = new Bucket(1, 20);

            List<KadContactNode> nodes = new List<KadContactNode>(20);
            for(int i = 0; i < 20; i++) {
                var contact = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
                nodes.Add(contact);
                bucket.Put(contact);
            }

            for(int i = 0; i < nodes.Count; i++){
                Debug.WriteLine(bucket.GetNode(nodes[i].Id));
            }
        }

        [TestMethod]
        public void Bucket_Contains() {
            Bucket bucket = new Bucket(1, 20);

            KadContactNode node = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
            bucket.Put(node);
            Assert.IsTrue(bucket.Contains(node.Id));
            Assert.IsFalse(bucket.Contains(KadId.GenerateRandom()));
        }

        [TestMethod]
        public void Bucket_GetLeastSeen() {
            Bucket bucket = new Bucket(1, 20);

            KadContactNode leastSeen = null;
            for(int i = 0; i < 20; i++) {
                var contact = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));                
                if(i == 0)
                    leastSeen = contact;
                bucket.Put(contact);
                Thread.Sleep(1000);
            }

            Assert.IsTrue(bucket.IsFull());
            Assert.IsTrue(leastSeen.Id.GetNumericValue() == bucket.GetLeastSeen().Id.GetNumericValue());
        }

        [TestMethod]
        public void Bucket_GetLeastSeen_Reversed() {
            Bucket bucket = new Bucket(1, 20);

            KadContactNode leastSeen = null;
            for(int i = 0; i < 20; i++) {
                var contact = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
                leastSeen = contact;
                bucket.Put(contact);
                Thread.Sleep(1000);
            }

            Assert.IsTrue(bucket.IsFull());
            Assert.IsFalse(leastSeen.Id.GetNumericValue() == bucket.GetLeastSeen().Id.GetNumericValue());
        }

        [TestMethod]
        public void Bucket_Replace() {
            Bucket bucket = new Bucket(1, 20);

            KadContactNode node1 = new KadContactNode(KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
            Thread.Sleep(1000);
            KadContactNode node2 = new KadContactNode(KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
            bucket.Put(node1);
            bucket.Put(node2);

            bucket.Replace(node1.Id, node2);

            Assert.IsTrue(bucket.NodesCount == 1);
            Assert.IsTrue(bucket.GetNodes().First().Id.GetNumericValue() == node2.Id.GetNumericValue());
        }

        [TestMethod]
        public void Bucket_SeenNow() {
            Bucket bucket = new Bucket(1, 20);

            KadContactNode node1 = new KadContactNode(KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
            Thread.Sleep(1000);
            KadContactNode node2 = new KadContactNode(KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
            bucket.Put(node1);
            bucket.Put(node2);

            bucket.SeenNow(node1);

            Assert.IsTrue(bucket.NodesCount == 2);
            Assert.IsTrue(bucket.GetLeastSeen().Id.GetNumericValue() == node2.Id.GetNumericValue());
        }
    }
}
