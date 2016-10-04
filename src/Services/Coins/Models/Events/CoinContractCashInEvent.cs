
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Services.Coins.Models.Events
{
	public class CoinContractCashInEvent
	{
		[Parameter("address", "caller", 1, true)]
		public string Caller { get; set; }

		[Parameter("uint", "amount", 2, false)]
		public BigInteger Amount { get; set; }
	}
}
