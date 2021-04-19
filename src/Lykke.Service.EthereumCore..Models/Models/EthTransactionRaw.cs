using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models.Models
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
