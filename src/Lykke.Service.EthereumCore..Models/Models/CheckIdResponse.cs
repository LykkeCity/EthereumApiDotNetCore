using System;
using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models
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
