using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models
{
    [DataContract]
    public class CurrentBlockModel
    {
        [DataMember]
        public ulong LatestBlockNumber{ get; set; }
    }
}
