using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCoreSelfHosted.Models
{
    [DataContract]
    public class ExternalTokenModel
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string ContractAddress { get; set; }
        public byte Divisibility { get; set; }
        public string Version { get; set; }
        public string TokenSymbol { get; set; }
        public string InitialSupply { get; set; }
    }
}