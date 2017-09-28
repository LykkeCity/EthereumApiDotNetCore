using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EthereumApi.Models.Models
{
    [DataContract]
    public class AddressTokenBalanceContainerResponse
    {
        [DataMember]
        public IEnumerable<AddressTokenBalanceResponse> Balances { get; set; }
    }

    [DataContract]
    public class AddressTokenBalanceResponse
    {
        [DataMember]
        public string Balance { get; set; }

        [DataMember]
        public string Erc20TokenAddress { get; set; }

        [DataMember]
        public string UserAddress { get; set; }
    }
}
