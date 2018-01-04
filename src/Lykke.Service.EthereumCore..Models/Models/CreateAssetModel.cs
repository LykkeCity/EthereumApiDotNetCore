using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Models
{
    public class CreateAssetModel
    {
        public string Blockchain { get; set; }
        public bool ContainsEth { get; set; }
        public string ExternalTokenAddress { get; set; }
        public int Multiplier { get; set; }
        public string Name { get; set; }
    }
}
