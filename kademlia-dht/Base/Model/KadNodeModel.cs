using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace kademlia_dht.Base.Model
{
    public class KadNodeModel {
        public string NodeIp { get; set; }
        public int NodePort { get; set; }
        public byte[] NodeId { get; set; }
        public List<ContactNodeModel> ContactNodes { get; set; }
        public List<StorageValueModel> StorageValues { get; set; }
        public List<StorageValueModel> OwnerValues { get; set; }

        public static KadNodeModel ForKadNode(KadNode kadNode){
            KadNodeModel nodeState = new KadNodeModel();
            nodeState.NodeIp = kadNode.EndPoint.Address.ToString();
            nodeState.NodePort = kadNode.EndPoint.Port;
            nodeState.NodeId = kadNode.Id.Value;
            nodeState.StorageValues = kadNode.Storage.Values
                .Select((v) => new StorageValueModel()
                {
                    ValueId = v.Id.Value,
                    Value = v.Value,
                    Timestamp = v.Timestamp,
                }).ToList();

            nodeState.OwnerValues = kadNode.Storage.OwnerValues
                .Select((v) => new StorageValueModel()
                {
                    ValueId = v.Id.Value,
                    Value = v.Value,
                    Timestamp = v.Timestamp,
                }).ToList();

            nodeState.ContactNodes = kadNode.BucketList.Buckets
                .SelectMany((b) => b.GetNodes())
                .Select((n) => new ContactNodeModel()
                {
                    ContactId = n.Id.Value,
                    IpAddress = n.EndPoint.Address.ToString(),
                    Port = n.EndPoint.Port
                }).ToList();

            return nodeState;
        }
    }
}
