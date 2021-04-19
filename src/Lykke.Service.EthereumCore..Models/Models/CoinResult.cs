using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models
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
