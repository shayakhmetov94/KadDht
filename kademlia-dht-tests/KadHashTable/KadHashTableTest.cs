using System;
using System.Linq;
using System.Net;
using System.Threading;
using kademlia_dht.Base;
using kademlia_dht.Base.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace kademlia_dht_tests.KadHashTableTests
{
    [TestClass]
    public class KadHashTableTest
    {

        [TestMethod]
        public void InitialBootstrap() {
            KadNode ownerNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode otherNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            byte[] baseId = new byte[20];
            baseId[0] = 255;
            for(int i = 0; i < 2; i++) { //fill up random bucket
                baseId[19] = (byte)i;
                KadContactNode contact = new KadContactNode(new KadId(baseId), new IPEndPoint(IPAddress.Loopback, 20000));
                otherNode.BucketList.Put(contact);
            }

            KadHashTable hashTable = new KadHashTable(ownerNode, new KadContactNode(otherNode.Id, otherNode.EndPoint));

            var contactsWithoutOther = ownerNode.BucketList.Buckets
                .SelectMany((b) => b.GetNodes())
                .Where((n) => n.Id.GetNumericValue() != otherNode.Id.GetNumericValue())
                .OrderBy((n) => n.Id.GetNumericValue())
                .Select((n) => n.Id.GetNumericValue())
                .ToList();

            var otherNodeContacts = otherNode.BucketList.Buckets
                .SelectMany((b) => b.GetNodes())
                .OrderBy((n) => n.Id.GetNumericValue())
                .Select((n) => n.Id.GetNumericValue())
                .ToList();

            CollectionAssert.AreEqual(contactsWithoutOther, otherNodeContacts);
        }


        [TestMethod]
        public void RefreshRange() {
            KadNode ownerNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode otherNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            byte[] baseId = new byte[20];
            baseId[0] = 255;
            for(int i = 0; i < 2; i++) { //fill up random bucket
                baseId[19] = (byte)i;
                KadContactNode contact = new KadContactNode(new KadId(baseId), new IPEndPoint(IPAddress.Loopback, 20000));
                otherNode.BucketList.Put(contact);
            }

            KadHashTableConfiguration cfg = new KadHashTableConfiguration() {
                BucketsRefreshInSecs = 5
            };

            KadHashTable hashTable = new KadHashTable(ownerNode, null, cfg);
            
            ownerNode.BucketList.Put(new KadContactNode(otherNode.Id, otherNode.EndPoint));
            Thread.Sleep(10000);

            var contactsWithoutOther = ownerNode.BucketList.Buckets
                .SelectMany((b) => b.GetNodes())
                .Where((n) => n.Id.GetNumericValue() != otherNode.Id.GetNumericValue())
                .OrderBy((n) => n.Id.GetNumericValue())
                .Select((n) => n.Id.GetNumericValue())
                .ToList();
             
            var otherNodeContacts = otherNode.BucketList.Buckets
                .SelectMany((b) => b.GetNodes())
                .OrderBy((n) => n.Id.GetNumericValue())
                .Select((n) => n.Id.GetNumericValue())
                .ToList();

            CollectionAssert.AreEqual(contactsWithoutOther, otherNodeContacts);
        }

        [TestMethod]
        public void ReplaceLeastSeen() {
            KadNode ownerNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode otherNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));

            KadContactNode irresponsiveContactWithSameId = new KadContactNode(otherNode.Id, new IPEndPoint(IPAddress.Loopback, 33333));
            
            ownerNode.BucketList.Put(irresponsiveContactWithSameId);

            KadHashTable hashTable = new KadHashTable(ownerNode);

            Assert.IsTrue(hashTable.TryReplaceLeastSeenContactFromBucket(new KadContactNode(otherNode.Id, otherNode.EndPoint)));

            KadContactNode otherNodeContact = ownerNode.BucketList.GetClosestNodes(otherNode.Id, 1).First();

            Assert.IsTrue(otherNodeContact.Id.GetNumericValue() == otherNode.Id.GetNumericValue());
            Assert.IsTrue(otherNodeContact.EndPoint.Port == otherNode.EndPoint.Port);

        }

        [TestMethod]
        public void StoreValue() {
            KadNode ownerNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55557));
            KadNode node3 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55558));
            KadNode node4 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55559));

            node1.BucketList.Put(new KadContactNode(node2.Id, node2.EndPoint));
            node1.BucketList.Put(new KadContactNode(node3.Id, node3.EndPoint));
            node1.BucketList.Put(new KadContactNode(node4.Id, node4.EndPoint));

            KadValue value = new KadValue(KadId.GenerateRandom(), DateTime.UtcNow, new byte[20]);

            KadHashTable hashTable = new KadHashTable(ownerNode, new KadContactNode(node1.Id, node1.EndPoint));

            hashTable.StoreValue(value);

            Assert.IsTrue(ownerNode.Storage.OwnerValues.Count() == 1);
            Assert.IsTrue(ownerNode.Storage.OwnerValues.First().Id.GetNumericValue() == value.Id.GetNumericValue());

            Assert.IsTrue(node1.Storage.Values.Count() == 1);
            Assert.IsTrue(node1.Storage.Values.First().Id.GetNumericValue() == value.Id.GetNumericValue());

            Assert.IsTrue(node2.Storage.Values.Count() == 1);
            Assert.IsTrue(node2.Storage.Values.First().Id.GetNumericValue() == value.Id.GetNumericValue());

            Assert.IsTrue(node3.Storage.Values.Count() == 1);
            Assert.IsTrue(node3.Storage.Values.First().Id.GetNumericValue() == value.Id.GetNumericValue());

            Assert.IsTrue(node4.Storage.Values.Count() == 1);
            Assert.IsTrue(node4.Storage.Values.First().Id.GetNumericValue() == value.Id.GetNumericValue());
        }

        [TestMethod]
        public void FindValue() {
            KadNode ownerNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNode node1 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55556));
            KadNode node2 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55557));
            KadNode node3 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55558));
            KadNode node4 = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55559));

            node1.BucketList.Put(new KadContactNode(node2.Id, node2.EndPoint));
            node1.BucketList.Put(new KadContactNode(node3.Id, node3.EndPoint));
            node1.BucketList.Put(new KadContactNode(node4.Id, node4.EndPoint));

            KadValue value = new KadValue(KadId.GenerateRandom(), DateTime.UtcNow, new byte[20]);

            KadHashTable hashTable = new KadHashTable(ownerNode, new KadContactNode(node1.Id, node1.EndPoint));

            hashTable.StoreValue(value);


            ownerNode.Storage.Remove(value.Id);
            var retrived = hashTable.FindValue(value.Id);
            
            Assert.IsTrue(retrived.Id.GetNumericValue() == value.Id.GetNumericValue());
        }


    }
}
