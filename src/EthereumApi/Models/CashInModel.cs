using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CashInModel
    {
	    [Required]
	    public string Coin { get; set; }

	    [Required]
		public string Receiver { get; set; }

		[Required]
		public decimal Amount { get; set; }
    }
}
