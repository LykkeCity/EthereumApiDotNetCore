using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models
{
    [DataContract]
    public class BalanceModel
    {
        [DataMember]
        public string Amount{ get; set; }
    }
}
