using Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class BaseCoinRequestModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string CoinAdapterAddress { get; set; }

        [Required]
        public string FromAddress { get; set; }

        [Required]
        public string ToAddress { get; set; }

        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Amount { get; set; }
    }
}
