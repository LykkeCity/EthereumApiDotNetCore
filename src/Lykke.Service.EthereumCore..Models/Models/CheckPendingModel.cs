using Lykke.Service.EthereumCore.Models.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Lykke.Service.EthereumCore.Models
{
    public class CheckPendingModel 
    {
        [Required]
        [EthereumAddress]
        public string CoinAdapterAddress{ get; set; }

        [Required]
        [EthereumAddress]
        public string UserAddress { get; set; }
    }

    public class CheckPendingResponse
    {
        [Required]
        public bool IsSynced{ get; set; }
    }
}
