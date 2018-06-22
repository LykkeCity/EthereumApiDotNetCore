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
    public class AirlinesTransferFromDepositRequest
    {
        [DataMember(Name = "depositAddress")]
        [Required]
        [EthereumAddress(allowsEmpty: false)]
        public string DepositContractAddress { get; set; }

        [DataMember(Name = "tokenAddress")]
        [Required]
        [EthereumAddress(allowsEmpty: false)]
        public string TokenAddress { get; set; }

        [DataMember(Name = "destinationAddress")]
        [Required]
        [EthereumAddress(allowsEmpty: false)]
        public string DestinationAddress { get; set; }

        [DataMember(Name = "tokenAmount")]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string TokenAmount { get; set; }
    }
}
