using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Models.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.EthereumCore.Models
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
