﻿using Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace EthereumApi.Models.Models
{
    [DataContract]
    public class PrivateWalletEthTransaction
    {
        [DataMember]
        [Required]
        public string FromAddress { get; set; }

        [DataMember]
        [Required]
        public string ToAddress { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string GasAmount { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string GasPrice { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Value { get; set; }
    }

    [DataContract]
    public class PrivateWalletEthSignedTransaction
    {
        [DataMember]
        [Required]
        public string FromAddress { get; set; }

        [DataMember]
        [Required]
        public string SignedTransactionHex { get; set; }
    }
}
