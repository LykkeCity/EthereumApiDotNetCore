using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models
{
    [DataContract]
    public class EstimatedGasModel
    {
        [DataMember]
        public string EstimatedGas { get; set; }

        [DataMember]
        public bool IsAllowed { get; set; }
    }

    [DataContract]
    public class EstimatedGasModelV2 : EstimatedGasModel
    {
        [DataMember]
        public string EthAmount { get; set; }

        [DataMember]
        public string GasPrice { get; set; }

    }
}
