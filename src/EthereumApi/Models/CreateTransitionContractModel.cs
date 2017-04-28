using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CreateTransitionContractModel
    {
        public string ExternalTokenAddress { get; set; }

        [Required]
        public string CoinAdapterAddress { get; set; }

        [Required]
        public string UserAddress { get; set; }
    }
}
