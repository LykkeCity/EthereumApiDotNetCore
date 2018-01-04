using Core;
using EthereumApi.Models.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class BaseCoinRequestModel : BaseCoinRequestParametersModel
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class BaseCoinRequestParametersModel
    {
        [Required]
        [EthereumAddress]
        public string CoinAdapterAddress { get; set; }

        [Required]
        [EthereumAddress]
        public string FromAddress { get; set; }

        [Required]
        [EthereumAddress]
        public string ToAddress { get; set; }

        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Amount { get; set; }
    }
}
