using System.Runtime.Serialization;

namespace EthereumApiSelfHosted.Models
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
        public byte Divisibility { get; internal set; }
        public string Version { get; internal set; }
        public string TokenSymbol { get; internal set; }
        public string InitialSupply { get; internal set; }
    }
}