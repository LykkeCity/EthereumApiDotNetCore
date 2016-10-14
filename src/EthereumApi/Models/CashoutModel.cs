using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
	public class CashoutModel : BaseCoinRequestModel
	{
		[Required]
		public string Coin { get; set; }

		[Required]
		public string Client { get; set; }

		[Required]
		public string To { get; set; }

		[Required]
		public decimal Amount { get; set; }

		[Required]
		public string Sign { get; set; }
	}
}
