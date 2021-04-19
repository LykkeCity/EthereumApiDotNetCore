using System;
using System.Linq;
using System.Numerics;
using Common;
using Nethereum.ABI.Encoders;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Lykke.Service.EthereumCore.Core.Utils
{
    public static class EthUtils
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

        public static BigInteger ToBlockchainAmount(this decimal amount, int multiplier)
        {
            if (multiplier == 0)
                throw new Exception("Multiplier is ZERO");
            var multy = new BigDecimal(BigInteger.Pow(10, multiplier), 0);
            var result = amount * multy;
            return (BigInteger)result;
        }

        public static decimal FromBlockchainAmount(this BigInteger amount, int multiplier)
        {
            if (multiplier == 0)
                throw new Exception("Multiplier is ZERO");
            var multy = BigInteger.Pow(10, multiplier);
            var result = new BigDecimal(amount, 0) / new BigDecimal(multy, 0);
            return (decimal)result;
        }
    }
}
