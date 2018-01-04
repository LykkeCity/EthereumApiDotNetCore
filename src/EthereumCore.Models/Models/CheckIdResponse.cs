using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    [DataContract]
    public class CheckIdResponse
    {
        [DataMember]
        public bool IsOk{ get; set; }

        [DataMember]
        public Guid ProposedId { get; set; }
    }
}
