using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using kademlia_dht.Base;
using kademlia_dht.Base.Model;
using kademlia_dht.Base.Storage;

namespace kademlia_dht.Base.Unit
{
    public class KadNodeUnit
    {
        private string _settingsPath = "nodesettings.xml";
        public KadNode Node { get; private set; }

        public KadNodeUnit() {
            InitFromFile();
        }

        public KadNodeUnit(KadNode node){
            if(node == null)
                throw new ArgumentNullException("node");
        
            Node = node;
        }

        public KadNodeUnit(string fromFile) {
            _settingsPath = fromFile;
            InitFromFile();
        }

        private void InitFromFile(){
            try {
                if(File.Exists(_settingsPath)) {
                    Node = LoadFromFile();
                    return;
                }
            } catch(Exception e){
                throw new Exception($"Can't load settings from file {_settingsPath}", e);
            }

            throw new Exception($"No settings file in {_settingsPath}");
        }

        private KadNode LoadFromFile() {
            KadNodeModel nodeState;
            using(var reader = new StreamReader(_settingsPath)) {
                var serializer = new XmlSerializer(typeof(KadNodeModel));
                nodeState = (KadNodeModel)serializer.Deserialize(reader);
            }

            KadNode node = new KadNode(new IPEndPoint(IPAddress.Parse(nodeState.NodeIp), nodeState.NodePort), new KadId(nodeState.NodeId));
            foreach(var storageVal in nodeState.StorageValues) {
                node.Storage.Put(new KadValue(new KadId(storageVal.ValueId), storageVal.Timestamp, storageVal.Value));
            }

            foreach(var ownerVal in nodeState.OwnerValues) {
                node.Storage.PutOwnerVal(new KadValue(new KadId(ownerVal.ValueId), ownerVal.Timestamp, ownerVal.Value));
            }

            foreach(var contactNode in nodeState.ContactNodes) {
                node.BucketList.Put(new KadContactNode(new KadId(contactNode.ContactId), new IPEndPoint(IPAddress.Parse(contactNode.IpAddress), contactNode.Port)));
            }

            return node;
        }

        public void SaveToFile(){
            SaveToFile(_settingsPath);
        }

        public void SaveToFile(string path) {
            //if(!Directory.Exists(path))
            //    throw new Exception( $"Can't save node data in {path}. Directory is inaccessible" );

            KadNodeModel nodeState = KadNodeModel.ForKadNode(Node);

            using(var writer = new StreamWriter(path)) {
                var serializer = new XmlSerializer(typeof(KadNodeModel));
                serializer.Serialize(writer, nodeState);
                writer.Flush();
            }
        }

        public void Stop(){
            Node.Shutdown();
        }

    }

}
