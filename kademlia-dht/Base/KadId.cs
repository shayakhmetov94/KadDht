using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace kademlia_dht.Base
{
    public class KadId
    {
        private BigInteger _numericVal = BigInteger.Zero;

        public byte[] Value { get; }

        public KadId(byte[] value) {
            if(value == null || value.Length < 20 || value.Length > 20)
                throw new ArgumentException("value");

            byte[] valueCopy = new byte[value.Length];
            Buffer.BlockCopy(value, 0, valueCopy, 0, value.Length);
            Value = valueCopy;
        }

        public BigInteger GetNumericValue() {
            if ( _numericVal.IsZero ) {
                byte[] valWithZeroByte = new byte[Value.Length + 1];
                if ( BitConverter.IsLittleEndian )
                    Buffer.BlockCopy( Value, 0, valWithZeroByte, 0, Value.Length );
                else
                    Buffer.BlockCopy( Value, 0, valWithZeroByte, 1, Value.Length );

                _numericVal = new BigInteger( valWithZeroByte );
            }

            return _numericVal;
        } 

        public static KadId operator ^( KadId first, KadId second ) {
            byte[] result = new byte[20];
            Buffer.BlockCopy( first.Value, 0, result, 0, 20 );
            for ( int i = 0; i < 20; i++ )
                result[i] ^= second.Value[i];

            return new KadId( result );
        }

        public static bool operator <( KadId first, KadId second ) {

            return first.GetNumericValue() < second.GetNumericValue();
        }

        public static bool operator >( KadId first, KadId second ) {

            return first.GetNumericValue() < second.GetNumericValue();
        }

        public static bool operator >=( KadId first, KadId second ) {

            return first.GetNumericValue() <= second.GetNumericValue();
        }

        public static bool operator <=( KadId first, KadId second ) {

            return first.GetNumericValue() <= second.GetNumericValue();
        }

        /// <summary>
        /// Generate random Id. Uses System.Security.Cryptography.RNGCryptoServiceProvider 
        /// </summary>
        /// <returns>New random Id</returns>
        public static KadId GenerateRandom() {
            var rnCryptoServiceProvider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            byte[] key = new byte[20];
            rnCryptoServiceProvider.GetBytes(key);
            return new KadId(key);
        }

        public class KadIdEqComparer : IEqualityComparer<KadId>
        {
            public bool Equals( KadId x, KadId y ) {
                return x.GetNumericValue() == y.GetNumericValue();
            }

            public int GetHashCode( KadId obj ) {
                return obj.GetNumericValue().GetHashCode();
            }
        }

        public class KadIdToBaseComparator : IComparer<KadContactNode>
        {
            private KadId _base;

            public KadIdToBaseComparator( KadId baseId ) {
                _base = baseId;
            }

            public int Compare( KadContactNode x, KadContactNode y ) {
                return _base.GetNumericValue().CompareTo( y.Id.GetNumericValue() );
            }
        }

        public class KadIdToIdComparator : IComparer<KadId>
        {
            public int Compare( KadId x, KadId y ) {
                return x.GetNumericValue().CompareTo( y.GetNumericValue() );
            }
        }
    }
}
