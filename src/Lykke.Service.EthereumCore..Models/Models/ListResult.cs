using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models
{
    [DataContract]
    public class ListResult<T>
    {
       [DataMember]
        public IEnumerable<T> Data { get; set; }
    }
}
