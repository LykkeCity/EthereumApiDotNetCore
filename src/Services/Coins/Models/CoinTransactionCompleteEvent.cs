using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Coins.Models
{
	public class CoinTransactionCompleteEvent
	{
		public string TransactionHash { get; set; }

		public int ConfirmationLevel { get; set; }

		public bool Error { get; set; }
	}
}
