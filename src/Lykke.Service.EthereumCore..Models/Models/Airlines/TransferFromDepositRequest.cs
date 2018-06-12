using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Models.Attributes;

namespace EthereumApi.Models.Models.Airlines
{
    [DataContract]
    public class TransferFromDepositRequest
    {
        [DataMember(Name = "depositAddress")]
        [EthereumAddress(allowsEmpty: false)]
        public string DepositContractAddress { get; set; }

        [DataMember(Name = "tokenAddress")]
        [EthereumAddress(allowsEmpty: false)]
        public string TokenAddress { get; set; }

        [DataMember(Name = "destinationAddress")]
        [EthereumAddress(allowsEmpty: false)]
        public string DestinationAddress { get; set; }

        [DataMember(Name = "destinationAddress")]
        [RegularExpression(Constants.BigIntTemplate)]
        public string TokenAmount { get; set; }
    }
}
