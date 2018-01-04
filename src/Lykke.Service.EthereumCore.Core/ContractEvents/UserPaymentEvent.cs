using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Lykke.Service.EthereumCore.Core.ContractEvents
{
	public class UserPaymentEvent
	{
		[Parameter("address", "userAddress", 1, true)]
		public string Address { get; set; }

		[Parameter("uint", "amount", 2, false)]
		public BigInteger Amount { get; set; }
	}
}
