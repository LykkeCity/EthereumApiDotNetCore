using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Models
{
    [DataContract]
    public class CurrentBlockModel
    {
        [DataMember]
        public ulong LatestBlockNumber{ get; set; }
    }
}
