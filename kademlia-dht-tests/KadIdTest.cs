using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Numerics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace kademlia_dht_tests
{
    [TestClass]
    public class IdTest
    {
        [TestMethod]
        public void IdXorTest() {
            kademlia_dht.Base.KadId kadId1 = new kademlia_dht.Base.KadId(SoapHexBinary.Parse("0000000000000000000000000000000000000000").Value);
            Thread.Sleep( 1000 );
            kademlia_dht.Base.KadId kadId2 = new kademlia_dht.Base.KadId(SoapHexBinary.Parse("000000000000000000000000000000000000000A").Value);

            Assert.IsTrue( (kadId1 ^ kadId2).GetNumericValue() > 0 );
            var pref = BigInteger.Log( (kadId1 ^ kadId2).GetNumericValue(), 2 );
            Assert.IsTrue( pref > 0 && pref <= 160 );
        }
    }
}
