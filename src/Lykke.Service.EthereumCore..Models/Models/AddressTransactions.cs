using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models.Models
{
    [DataContract]
    public class AddressTransactions
    {
        [DataMember]
        [Required]
        public string Address { get; set; }

        [DataMember]
        public int Start { get; set; }

        [DataMember]
        public int Count { get; set; }
    }

    [DataContract]
    public class TokenAddressTransactions : AddressTransactions
    {
        [DataMember]
        public string TokenAddress { get; set; }
    }
}
