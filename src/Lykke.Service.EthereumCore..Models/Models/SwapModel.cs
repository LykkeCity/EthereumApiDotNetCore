﻿using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.EthereumCore.Models
{
    public class SwapModel : BaseCoinRequestModel
	{
	    [Required]
		public string ClientA { get; set; }

		[Required]
		public string ClientB { get; set; }

		[Required]
		public string CoinA { get; set; }

		[Required]
		public string CoinB { get; set; }

		[Required]
		public decimal AmountA { get; set; }

		[Required]
		public decimal AmountB { get; set; }

		[Required]
		public string SignA { get; set; }

		[Required]
		public string SignB { get; set; }
    }
}
