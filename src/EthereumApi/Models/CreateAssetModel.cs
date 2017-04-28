using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CreateAssetModel
    {
        public string Abi { get; internal set; }
        public string AdapterAddress { get; internal set; }
        public string Blockchain { get; internal set; }
        public string Bytecode { get; internal set; }
        public int Multiplier { get; internal set; }
        public string Name { get; internal set; }
    }
}
