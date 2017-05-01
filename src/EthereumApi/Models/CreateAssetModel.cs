using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CreateAssetModel
    {
        public string Abi { get; set; }
        public string Blockchain { get; set; }
        public string Bytecode { get; set; }
        public int Multiplier { get; set; }
        public string Name { get; set; }
    }
}
