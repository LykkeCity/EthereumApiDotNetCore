using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CashoutModel
    {
		public string Coin { get; set; }
		public string Client { get; set; }
		public string To { get; set; }
		public decimal Amount { get; set; }
		public string Sign { get; set; }
    }
}
