using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

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
