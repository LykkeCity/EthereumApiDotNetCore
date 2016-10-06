using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.Encoders;
using Nethereum.Hex.HexConvertors.Extensions;

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
		    return new IntTypeEncoder().EncodeInt(GuidToBigInteger(guid));
	    }
		
	    public static BigInteger GuidToBigInteger(Guid guid)
	    {
			// for always positive BigInteger
		    var s = "00" + guid.ToString("n");
		    return new BigInteger(s.HexToByteArray().Reverse().ToArray());
	    }
    }
}
