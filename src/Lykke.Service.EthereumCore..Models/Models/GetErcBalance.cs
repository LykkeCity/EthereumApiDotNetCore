﻿using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Lykke.Service.EthereumCore.Models.Models
{
    [DataContract]
    public class GetErcBalance
    {
        [DataMember] 
        public string Address { get; set; }

        [DataMember]
        public IEnumerable<string> TokenAddresses { get; set; }
    }
}
