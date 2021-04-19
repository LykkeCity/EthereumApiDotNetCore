using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.PassTokenIntegration.Models.Requests
{
    [DataContract]
    public class EthAddressRequest
    {
        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "addressType")]
        public string AddressType { get; set; }
    }
}
