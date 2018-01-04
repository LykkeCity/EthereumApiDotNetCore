using Core;
using EthereumApi.Models.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class IssueTokensModel
    {
        [Required]
        [EthereumAddress]
        public string ExternalTokenAddress { get; set; }

        [Required]
        [EthereumAddress]
        public string ToAddress { get; set; }

        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Amount { get; set; }
    }
}
