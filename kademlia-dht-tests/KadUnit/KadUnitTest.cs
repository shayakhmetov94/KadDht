using System;
using System.Linq;
using System.Net;
using System.Threading;
using kademlia_dht.Base;
using kademlia_dht.Base.Buckets;
using kademlia_dht.Base.Storage;
using kademlia_dht.Base.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kademlia_dht_tests.KadUnit
{
    [TestClass]
    public class KadUnitTest
    {
        [TestMethod]
        public void KadNodeUnit_Save() {
            KadNode kadNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNodeUnit unit = new KadNodeUnit(kadNode);
            unit.SaveToFile();
            unit.Stop();
        }

        [TestMethod]
        public void KadNodeUnit_Load() {
            KadNodeUnit unit = new KadNodeUnit();
            unit.Stop();
        }

        [TestMethod]
        public void KadNodeUnit_SaveLoadBuckets() {
            KadNode kadNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNodeUnit unit = new KadNodeUnit(kadNode);

            for(int i = 0; i < 20; i++) {
                var contact = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
                unit.Node.BucketList.Put(contact);
                Thread.Sleep(1000);
            }
            string savePath = "nodewithbuckets.xml";
            unit.SaveToFile(savePath);
            unit.Stop();

            KadNodeUnit unit2 = new KadNodeUnit(savePath);

            Assert.IsTrue(unit2.Node.Id.GetNumericValue() == unit.Node.Id.GetNumericValue());
            Assert.IsTrue(unit2.Node.EndPoint.Address.ToString() == unit.Node.EndPoint.Address.ToString());
            Assert.IsTrue(unit2.Node.EndPoint.Port == unit.Node.EndPoint.Port);
            CollectionAssert.AreEqual(unit2.Node.BucketList
                               .Buckets.Select((b) => b.Id).ToList(), unit.Node.BucketList.Buckets.Select((b) => b.Id).ToList());

            CollectionAssert.AreEqual(unit2.Node.BucketList
                               .Buckets
                                    .SelectMany((b) => b.GetNodes())
                                    .Select((n) => n.Id.GetNumericValue())
                                    .OrderBy((id) => id)
                                    .ToList(),
                                unit.Node.BucketList
                                    .Buckets
                                        .SelectMany((b) => b.GetNodes())
                                        .Select((n) => n.Id.GetNumericValue())
                                        .OrderBy((id) => id)
                                        .ToList());

        }

        [TestMethod]
        public void KadNodeUnit_SaveLoadBucketsAndValues() {
            KadNode kadNode = new KadNode(new System.Net.IPEndPoint(IPAddress.Loopback, 55555));
            KadNodeUnit unit = new KadNodeUnit(kadNode);

            for(int i = 0; i < 20; i++) {
                var contact = new kademlia_dht.Base.KadContactNode(kademlia_dht.Base.KadId.GenerateRandom(), new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
                unit.Node.BucketList.Put(contact);
                Thread.Sleep(1000);
            }

            byte[] value = new byte[20];
            for(int i = 0; i < 20; i++) {
                value[0] = (byte)(i + 1);
                var valId = new KadId(value);
                value[19] = (byte)i;
                var kadValue = new KadValue(valId, DateTime.UtcNow, value);

                if(i % 3 == 0) {
                    byte[] ownId = new byte[20];
                    ownId[3] = (byte)i;
                    unit.Node.Storage.PutOwnerVal(new KadValue(new KadId(ownId), DateTime.UtcNow, value));
                } else
                    unit.Node.Storage.Put(kadValue);

                Thread.Sleep(1000);
            }

            string savePath = "nodewithbuckets.xml";
            unit.SaveToFile(savePath);
            unit.Stop();

            KadNodeUnit unit2 = new KadNodeUnit(savePath);

            Assert.IsTrue(unit2.Node.Id.GetNumericValue() == unit.Node.Id.GetNumericValue());
            Assert.IsTrue(unit2.Node.EndPoint.Address.ToString() == unit.Node.EndPoint.Address.ToString());
            Assert.IsTrue(unit2.Node.EndPoint.Port == unit.Node.EndPoint.Port);
            CollectionAssert.AreEqual(unit2.Node.BucketList
                               .Buckets.Select((b) => b.Id).ToList(), unit.Node.BucketList.Buckets.Select((b) => b.Id).ToList());

            CollectionAssert.AreEqual(unit2.Node.BucketList
                               .Buckets
                                    .SelectMany((b) => b.GetNodes())
                                    .Select((n) => n.Id.GetNumericValue())
                                    .OrderBy((id) => id)
                                    .ToList(),
                                unit.Node.BucketList
                                    .Buckets
                                        .SelectMany((b) => b.GetNodes())
                                        .Select((n) => n.Id.GetNumericValue())
                                        .OrderBy((id) => id)
                                        .ToList());
            CollectionAssert.AreEqual(unit2.Node.Storage.Values
                        .Select((v) => v.Id.GetNumericValue())
                        .OrderBy((id) => id)
                        .ToList(),
                        unit.Node.Storage.Values
                        .Select((v) => v.Id.GetNumericValue())
                        .OrderBy((id) => id)
                        .ToList());

            CollectionAssert.AreEqual(unit2.Node.Storage.OwnerValues
                                .Select((v) => v.Id.GetNumericValue())
                                .OrderBy((id) => id)
                                .ToList(),
                                unit.Node.Storage.OwnerValues
                                .Select((v) => v.Id.GetNumericValue())
                                .OrderBy((id) => id)
                                .ToList());

            CollectionAssert.AreEqual(unit2.Node.Storage.OwnerValues
                                .Select((v) => v.Timestamp)
                                .OrderBy((t) => t)
                                .ToList(),
                                unit.Node.Storage.OwnerValues
                                .Select((v) => v.Timestamp)
                                .OrderBy((t) => t)
                                .ToList());
        }

    }
}
