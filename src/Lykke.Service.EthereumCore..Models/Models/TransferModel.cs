using Lykke.Service.EthereumCore.Core;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.EthereumCore.Models
{
    public class TransferModel : BaseCoinRequestModel
    {
        public string Sign { get; set; }
    }

    public class TransferWithChangeModel : BaseCoinRequestModel
    {
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Change { get; set; }

        [Required]
        public string SignFrom { get; set; }

        public string SignTo { get; set; }
    }

    public class CheckSignModel : BaseCoinRequestModel
    {
        [Required]
        public string Sign { get; set; }
    }
}
