using EthereumApi.Models.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EthereumApi.Models
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
