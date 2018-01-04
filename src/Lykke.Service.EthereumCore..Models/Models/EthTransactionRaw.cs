using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;

namespace EthereumApi.Models.Models
{
    [DataContract]
    public class EthTransactionRaw
    {
        [DataMember]
        public string FromAddress { get; set; }

        [DataMember]
        public string TransactionHex { get; set; }
    }
}
