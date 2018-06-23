using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base
{
    public class KadHashTableConfiguration
    {
        public int? ReplicationCount { get; set; }
        public int? MaxConcurrentThreads { get; set; }
        public int? ReplicationInSecs { get; set; }
        public int? RepublicationInSecs { get; set; }
        public int? BucketsRefreshInSecs{ get; set; }

        public static KadHashTableConfiguration ForKadHashTable(KadHashTable hashTable){
            KadHashTableConfiguration tableState = new KadHashTableConfiguration();

            tableState.ReplicationCount = hashTable.ReplicationCount;
            tableState.MaxConcurrentThreads = hashTable.MaxConcurrentThreads;
            tableState.ReplicationInSecs = hashTable.ReplicationInSecs;
            tableState.RepublicationInSecs = hashTable.RepublicationInSecs;
            tableState.BucketsRefreshInSecs = hashTable.BucketsRefreshInSecs;

            return tableState;
        }
    }
}
