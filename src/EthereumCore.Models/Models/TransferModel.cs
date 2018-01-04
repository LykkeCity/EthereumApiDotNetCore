using Lykke.Service.EthereumCore.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
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
