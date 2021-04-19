using Lykke.Service.EthereumCore.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models.Models
{
    [DataContract]
    public class EthereumAddressRequest
    {
        [DataMember]
        [Required(AllowEmptyStrings = false)]
        [EthereumAddress]
        public string Address { get; set; }
    }

    [DataContract]
    public class EthereumAddressResponse
    {
        [DataMember]
        public string Address { get; set; }
    }
}
