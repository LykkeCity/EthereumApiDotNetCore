using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.Encoders;

namespace Core.Utils
{
    public class EthUtils
    {
		public static byte[] IntToArrayWithPadding(int i)
		{
			return new IntTypeEncoder().EncodeInt(i);
		}

		public static byte[] BigIntToArrayWithPadding(BigInteger b)
	    {
			return new IntTypeEncoder().EncodeInt(b);
		}

	    public static byte[] GuidToByteArray(Guid guid)
	    {
		    var b = new BigInteger(guid.ToByteArray());
		    return new IntTypeEncoder().EncodeInt(b);
	    }
    }
}
