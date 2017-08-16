using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;

namespace EthereumApi.Models.Models
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
