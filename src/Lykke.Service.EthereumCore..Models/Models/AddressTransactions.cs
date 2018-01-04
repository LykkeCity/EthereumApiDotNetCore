using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace EthereumApi.Models.Models
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
        [Required]
        public string TokenAddress { get; set; }
    }
}
