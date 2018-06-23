using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base.Model
{
    public class ContactNodeModel
    {
        public byte[] ContactId { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
    }
}
