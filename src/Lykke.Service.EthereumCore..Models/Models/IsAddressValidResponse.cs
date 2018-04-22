using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Models.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace Lykke.Service.EthereumCore.Models.Models
{
    [DataContract]
    public class IsAddressValidResponse
    {
        [DataMember]
        public bool IsValid { get; set; }
    }
}
