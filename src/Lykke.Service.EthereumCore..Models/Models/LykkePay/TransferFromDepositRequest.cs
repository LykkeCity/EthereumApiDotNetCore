using System.Runtime.Serialization;
using Lykke.Service.EthereumCore.Models.Attributes;

namespace EthereumApi.Models.Models.LykkePay
{
    [DataContract]
    public class TransferFromDepositRequest
    {
        [DataMember(Name = "depositAddress")]
        [EthereumAddress(allowsEmpty: false)]
        public string DepositContractAddress { get; set; }

        [DataMember(Name = "tokenAddress")]
        [EthereumAddress(allowsEmpty: false)]
        public string TokenAddress { get; set; }

        [DataMember(Name = "destinationAddress")]
        [EthereumAddress(allowsEmpty: false)]
        public string DestinationAddress { get; set; }
    }
}
