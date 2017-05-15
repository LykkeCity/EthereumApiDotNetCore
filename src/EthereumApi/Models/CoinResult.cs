using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    [DataContract]
    public class CoinResult
    {
        [DataMember]
        public string Blockchain { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string AdapterAddress { get; set; }

        [DataMember]
        public string ExternalTokenAddress { get; set; }

        [DataMember]
        public int Multiplier { get; set; }

        [DataMember]
        public bool BlockchainDepositEnabled { get; set; }

        [DataMember]
        public bool ContainsEth { get; set; }
    }
}
