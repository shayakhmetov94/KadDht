using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Model
{
    public class StorageValueModel {

        public byte[] ValueId { get; set; }
        public byte[] Value { get; set; }
        public DateTime Timestamp {get; set;}
    }
}
