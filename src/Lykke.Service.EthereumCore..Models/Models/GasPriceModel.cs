using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Lykke.Service.EthereumCore.Core;

namespace Lykke.Service.EthereumCore.Models.Models
{
    public class GasPriceModel
    {
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Max { get; set; }

        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Min { get; set; }
    }
}