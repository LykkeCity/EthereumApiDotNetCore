﻿using EthereumApi.Models.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace EthereumApi.Models.Models
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
        public string Amount { get; set; }

        [DataMember]
        [EthereumAddress(allowsEmpty: true)]
        public string TokenAddress { get; set; }
    }

}
