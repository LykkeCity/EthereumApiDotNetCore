using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EthereumApi.Models.Models.LykkePay
{
    [DataContract]
    public class TransferFromDepositRequest
    {
        [DataMember(Name = "userAddress")]
        public string UserAddress { get; set; }

        [DataMember(Name = "tokenAddress")]
        public string TokenAddress { get; set; }

        [DataMember(Name = "destinationAddress")]
        public string DestinationAddress { get; set; }
    }
}
