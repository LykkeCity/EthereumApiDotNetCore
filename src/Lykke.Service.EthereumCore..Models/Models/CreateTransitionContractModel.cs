using Lykke.Service.EthereumCore.Models.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Models
{
    public class CreateTransitionContractModel
    {
        [Required]
        [EthereumAddress]
        public string CoinAdapterAddress { get; set; }

        [Required]
        [EthereumAddress]
        public string UserAddress { get; set; }

    }
}
