using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Models
{
    public class AddCoinModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Blockchain { get; set; }
        [Required]
        public int Multiplier { get; set; }

        [Required]
        public bool BlockchainDepositEnabled { get; set; }

        [Required]
        public string Abi { get; set; }

        [Required]
        public string ByteCode { get; set; }

    }
}
