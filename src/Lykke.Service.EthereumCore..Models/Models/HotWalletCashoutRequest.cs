﻿using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models.Models
{
    [DataContract]
    public class HotWalletCashoutRequest
    {
        [DataMember]
        [Required]
        public string OperationId { get; set; }

        [DataMember]
        [EthereumAddress]
        public string FromAddress { get; set; }

        [DataMember]
        [EthereumAddress]
        public string ToAddress { get; set; }

        [DataMember]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Amount { get; set; }

        [DataMember]
        [EthereumAddress]
        public string TokenAddress { get; set; }
    }

}
