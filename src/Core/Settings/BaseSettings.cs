using System;
using System.Collections.Generic;
using System.Numerics;

namespace Core.Settings
{
	public interface IBaseSettings
	{
		string EthereumPrivateAccount { get; set; }
		string EthereumMainAccount { get; set; }
		string EthereumMainAccountPassword { get; set; }

		string EthereumMainContractAddress { get; set; }
		string EthereumMainExchangeContractAddress { get; set; }

		string EthereumUrl { get; set; }

		DbSettings Db { get; set; }

		int MinContractPoolLength { get; set; }
		int MaxContractPoolLength { get; set; }
		int ContractsPerRequest { get; set; }
		decimal MainAccountMinBalance { get; set; }

		int Level1TransactionConfirmation { get; set; }
		int Level2TransactionConfirmation { get; set; }
		int Level3TransactionConfirmation { get; set; }

		EthereumContract MainContract { get; set; }
		EthereumContract UserContract { get; set; }
		EthereumContract MainExchangeContract { get; set; }

		Dictionary<string, EthereumContract> CoinContracts { get; set; }
	}

	public class BaseSettings : IBaseSettings
	{
		public EthereumContract MainContract { get; set; }
		public EthereumContract UserContract { get; set; }
		public EthereumContract MainExchangeContract { get; set; }

		public Dictionary<string, EthereumContract> CoinContracts { get; set; }

		public string EthereumPrivateAccount { get; set; }

		public string EthereumMainAccount { get; set; }
		public string EthereumMainAccountPassword { get; set; }

		/// <summary>
		/// Ethereum main contract (which fires event) address
		/// </summary>
		public string EthereumMainContractAddress { get; set; }

		public string EthereumMainExchangeContractAddress { get; set; }
		public string EthereumEthCoinContract { get; set; }

		/// <summary>
		/// Ethereum geth URL
		/// </summary>
		public string EthereumUrl { get; set; }

		public DbSettings Db { get; set; }

		public int MinContractPoolLength { get; set; } = 100;
		public int MaxContractPoolLength { get; set; } = 200;
		public int ContractsPerRequest { get; set; } = 50;
		public decimal MainAccountMinBalance { get; set; } = 1.0m;

		public int Level1TransactionConfirmation { get; set; } = 2;
		public int Level2TransactionConfirmation { get; set; } = 20;
		public int Level3TransactionConfirmation { get; set; } = 100;
	}

	public class EthereumContract
	{
		public string Name { get; set; }
		public string Abi { get; set; }
		public string ByteCode { get; set; }
		public string Multiplier { get; set; }

		public BigInteger GetInternalValue(decimal i)
		{
			int countPlaces = BitConverter.GetBytes(decimal.GetBits(i)[3])[2];

			if (countPlaces > 10)
				throw new ArgumentOutOfRangeException(nameof(i));

			var pow = (long)Math.Pow(10, countPlaces);
			var number = i * pow;

			var multiply = BigInteger.Multiply(BigInteger.Parse(Multiplier ?? "1"), new BigInteger(number));
			return BigInteger.Divide(multiply, new BigInteger(pow));
		}
	}

	public class DbSettings
	{
		public string DataConnString { get; set; }
		public string LogsConnString { get; set; }


		public string ExchangeQueueConnString { get; set; }
		public string EthereumNotificationsConnString { get; set; }
	}
}
