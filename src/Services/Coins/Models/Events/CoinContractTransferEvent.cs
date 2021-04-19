using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Lykke.Service.EthereumCore.Services.Coins.Models.Events
{
	public class CoinContractTransferEvent
	{
		[Parameter("address", "caller", 1, true)]
		public string Caller { get; set; }

		[Parameter("address", "from", 2, true)]
		public string From { get; set; }

		[Parameter("address", "to", 3, true)]
		public string To { get; set; }

		[Parameter("uint", "amount", 4, false)]
		public BigInteger Amount { get; set; }
	}
}
