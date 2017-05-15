using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    [DataContract]
    public class BalanceModel
    {
        [DataMember]
        public string Amount{ get; set; }
    }
}
