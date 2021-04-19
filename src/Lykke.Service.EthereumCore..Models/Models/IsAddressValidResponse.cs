using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models.Models
{
    [DataContract]
    public class IsAddressValidResponse
    {
        [DataMember]
        public bool IsValid { get; set; }
    }
}
