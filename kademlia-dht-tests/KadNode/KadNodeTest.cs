using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading;
using kademlia_dht.Base;
using kademlia_dht.Base.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kademlia_dht_tests.KadNodeTests
{
    [TestClass]
    public class KadNodeTest
    {
        [TestMethod]
        public void RetrieveContacts() {
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            List<KadContactNode> nodes = new List<KadContactNode>(20);
            for(int i = 0; i < 20; i++) {
                var contact = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
                nodes.Add(contact);
                node2.BucketList.Put(contact);
                Thread.Sleep(500);
            }
            nodes = nodes.OrderBy((n) => n.Id.GetNumericValue()).ToList();
            node1.BucketList.Put(new KadContactNode(node2.Id, node2.EndPoint));
            var foundNodes = node1.FindNode(node2.Id, KadId.GenerateRandom());
            CollectionAssert.AreEqual(foundNodes.Select((n) => n.Id.GetNumericValue()).OrderBy((n)=>n).ToList(),
             nodes.Select((n) => n.Id.GetNumericValue()).OrderBy((n) => n).ToList());

            node1.Shutdown();
            node2.Shutdown();
        }

        [TestMethod]
        public void RetrieveValues() {
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            List<KadValue> values = new List<KadValue>(20);
            for(int i = 0; i < 20; i++) {
                var value = new KadValue(kademlia_dht.Base.KadId.GenerateRandom(), DateTime.UtcNow, new byte[20]);
                values.Add(value);
                node2.Storage.Put(value);
                Thread.Sleep(500);
            }

            values = values.OrderBy((v) => v.Id.GetNumericValue()).ToList();
            node1.BucketList.Put(new KadContactNode(node2.Id, node2.EndPoint));
            var foundVal = node1.FindValue(node2.Id, values.First().Id);

            Assert.AreEqual(values.First().Id.GetNumericValue(), foundVal.Value.Id.GetNumericValue());

            node1.Shutdown();
            node2.Shutdown();
        }

        [TestMethod]
        public void RetrieveValues_NoValuesInStorage() {
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            List<KadContactNode> nodes = new List<KadContactNode>(20);
            for(int i = 0; i < 20; i++) {
                var contact = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
                nodes.Add(contact);
                node2.BucketList.Put(contact);
                Thread.Sleep(500);
            }
            nodes = nodes.OrderBy((n) => n.Id.GetNumericValue()).ToList();
            node1.BucketList.Put(new KadContactNode(node2.Id, node2.EndPoint));

            var foundNodes = node1.FindValue(node2.Id, KadId.GenerateRandom()).Contacts;

            CollectionAssert.AreEqual(foundNodes.Select((n) => n.Id.GetNumericValue()).OrderBy((n) => n).ToList(),
            nodes.Select((n) => n.Id.GetNumericValue()).OrderBy((n) => n).ToList());

            node1.Shutdown();
            node2.Shutdown();
        }

        [TestMethod]
        public void StoreValue() {
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            KadValue val = new KadValue(KadId.GenerateRandom(), DateTime.Now, new byte[20]);
            
            node1.BucketList.Put(new KadContactNode(node2.Id, node2.EndPoint));
            Assert.IsTrue(node1.StoreValue(node2.Id, val));

            KadValue node2Val = node2.Storage.Get(val.Id);

            Assert.IsTrue(val.Id.GetNumericValue() == node2Val.Id.GetNumericValue());

            node1.Shutdown();
            node2.Shutdown();
        }

        [TestMethod]
        public void StoreValue_CantStore() {
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            for(int i = 0; i < 20; i++) {
                var value = new KadValue(kademlia_dht.Base.KadId.GenerateRandom(), DateTime.UtcNow, new byte[20]);
                node2.Storage.Put(value);
                Thread.Sleep(500);
            }

            KadValue val = new KadValue(KadId.GenerateRandom(), DateTime.UtcNow, new byte[20]);

            node1.BucketList.Put(new KadContactNode(node2.Id, node2.EndPoint));
            Assert.IsFalse(node1.StoreValue(node2.Id, val));

            node1.Shutdown();
            node2.Shutdown();
        }

        [TestMethod]
        public void IsValueExpired_GoodAmountOfCloseContacts() {
            KadId ownerNodeId = KadId.GenerateRandom();
            
            for(int i = 2; i < ownerNodeId.Value.Length; i++)
                ownerNodeId.Value[i] = 255;
            ownerNodeId.Value[0] = 0;
            ownerNodeId.Value[1] = 0;
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555), ownerNodeId);


            byte[] baseId = new byte[20];
            baseId[0] = 255;
            for(int i = 0; i < 20; i++) { //fill up random bucket
                baseId[19] = (byte)i;
                KadContactNode contact = new KadContactNode(new KadId(baseId), new IPEndPoint(IPAddress.Loopback, 20000));
                node1.BucketList.Put(contact);
            }

            baseId[1] = 255;
            for(int i = 0; i < 20; i++) { 
                baseId[19] = (byte)i;
                KadContactNode contact = new KadContactNode(new KadId(baseId), new IPEndPoint(IPAddress.Loopback, 20000));
                node1.BucketList.Put(contact);
            }

            KadValue value = new KadValue(node1.BucketList.Buckets.Aggregate((o, n) => o.Id < n.Id ? n : o).GetNodes().Aggregate((o, n) => o.Id > n.Id ? n : o).Id, DateTime.UtcNow, new byte[20]);
            
            Assert.IsFalse(node1.IsValueExpired(value));

            KadValue expiredVal = new KadValue(value.Id, value.Timestamp.AddSeconds(-(node1.ValueExpirationInSec + 1)), new byte[20]);

            Assert.IsTrue(node1.IsValueExpired(expiredVal));

            node1.Shutdown();
        }

        [TestMethod]
        public void IsValueExpired_Exp() {
            KadId ownerNodeId = KadId.GenerateRandom();

            for(int i = 2; i < ownerNodeId.Value.Length; i++)
                ownerNodeId.Value[i] = 255;
            ownerNodeId.Value[0] = 0;
            ownerNodeId.Value[1] = 0;
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555), ownerNodeId);

            byte[] baseId = new byte[20];
            
            for(int i = 1; i < 21; i++) { //fill up random bucket
                baseId[19] = (byte)i;
                KadContactNode contact = new KadContactNode(new KadId(baseId), new IPEndPoint(IPAddress.Loopback, 20000));
                node1.BucketList.Put(contact);
            }

            //baseId[0] = 255;
            //baseId[1] = 255;
            //baseId[19] = 0;
            //for(int i = 1; i < 20; i++) { 
            //    baseId[3] = (byte)i;
            //    ContactNode contact = new ContactNode(new KadId(baseId), new IPEndPoint(IPAddress.Loopback, 20000));
            //    node1.BucketList.Put(contact);
            //}

            KadValue nonExpiredVal = new KadValue(node1.BucketList.Buckets.Aggregate((o, n) => o.Id < n.Id ? n : o).GetNodes()
                    .Aggregate((o, n)=> o.Id > n.Id ? n : o).Id, DateTime.UtcNow.AddSeconds(-(node1.ValueExpirationInSec + 1)), new byte[20]);

            Assert.IsFalse(node1.IsValueExpired(nonExpiredVal));

            KadValue expiredVal = new KadValue(nonExpiredVal.Id, DateTime.UtcNow.AddSeconds(-(((node1.ValueExpirationInSec + 1) * Math.Exp(20/19)))), new byte[20]);

            Assert.IsTrue(node1.IsValueExpired(expiredVal));

            node1.Shutdown();
        }


    }
}
