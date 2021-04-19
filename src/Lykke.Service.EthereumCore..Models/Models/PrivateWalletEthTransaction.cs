using Lykke.Service.EthereumCore.Core;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.Models.Models
{
    [DataContract]
    public class EthTransactionBase
    {
        [DataMember]
        [Required]
        public string FromAddress { get; set; }

        [DataMember]
        [Required]
        public string ToAddress { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string GasAmount { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string GasPrice { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public virtual string Value { get; set; }
    }

    [DataContract]
    public class PrivateWalletEthTransaction : EthTransactionBase
    {
        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string Value { get; set; }
    }

    [DataContract]
    public class PrivateWalletDataTransaction : EthTransactionBase
    {
        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntAllowZeroTemplate)]
        public override string Value { get; set; }

        //AnyData -_-
        [DataMember]
        public string Data { get; set; }
    }

    [DataContract]
    public class PrivateWalletErc20Transaction : EthTransactionBase
    {
        [DataMember]
        [Required]
        public string TokenAddress { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string TokenAmount { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntAllowZeroTemplate)]
        public override string Value { get; set; }
    }

    [DataContract]
    public class PrivateWalletEthSignedTransaction
    {
        [DataMember]
        [Required]
        public string FromAddress { get; set; }

        [DataMember]
        [Required]
        public string SignedTransactionHex { get; set; }
    }

    [DataContract]
    public class PrivateWalletEstimateTransaction
    {
        [DataMember]
        [Required]
        public string FromAddress { get; set; }

        [DataMember]
        [Required]
        public string ToAddress { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string EthAmount { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string GasPrice { get; set; }
    }

    [DataContract]
    public class PrivateWalletErc20EstimateTransaction : PrivateWalletEstimateTransaction
    {
        [DataMember]
        [Required]
        public string TokenAddress { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntTemplate)]
        public string TokenAmount { get; set; }

        [DataMember]
        [Required]
        [RegularExpression(Constants.BigIntAllowZeroTemplate)]
        public string EthAmount { get; set; }
    }
}
