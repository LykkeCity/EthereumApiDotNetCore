
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Services.Coins.Models.Events
{
    public class CoinContractCashOutEvent
    {
		[Parameter("address", "caller", 1, true)]
		public string Caller { get; set; }

		[Parameter("address", "from", 2, true)]
		public string From { get; set; }

		[Parameter("uint", "amount", 3, false)]
		public BigInteger Amount { get; set; }

		[Parameter("address", "to", 4, true)]
		public string To { get; set; }
	}
}
