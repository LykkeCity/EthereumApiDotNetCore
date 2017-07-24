// Code generated by Microsoft (R) AutoRest Code Generator 1.0.1.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace EthereumSamuraiApiCaller.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class TransactionResponse
    {
        /// <summary>
        /// Initializes a new instance of the TransactionResponse class.
        /// </summary>
        public TransactionResponse()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the TransactionResponse class.
        /// </summary>
        public TransactionResponse(int? transactionIndex = default(int?), long? blockNumber = default(long?), string gas = default(string), string gasPrice = default(string), string value = default(string), string nonce = default(string), string transactionHash = default(string), string blockHash = default(string), string fromProperty = default(string), string to = default(string), string input = default(string), int? blockTimestamp = default(int?), string contractAddress = default(string), string gasUsed = default(string), bool? hasError = default(bool?))
        {
            TransactionIndex = transactionIndex;
            BlockNumber = blockNumber;
            Gas = gas;
            GasPrice = gasPrice;
            Value = value;
            Nonce = nonce;
            TransactionHash = transactionHash;
            BlockHash = blockHash;
            FromProperty = fromProperty;
            To = to;
            Input = input;
            BlockTimestamp = blockTimestamp;
            ContractAddress = contractAddress;
            GasUsed = gasUsed;
            HasError = hasError;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transactionIndex")]
        public int? TransactionIndex { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "blockNumber")]
        public long? BlockNumber { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "gas")]
        public string Gas { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "gasPrice")]
        public string GasPrice { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transactionHash")]
        public string TransactionHash { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "blockHash")]
        public string BlockHash { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "from")]
        public string FromProperty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "input")]
        public string Input { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "blockTimestamp")]
        public int? BlockTimestamp { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "contractAddress")]
        public string ContractAddress { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "gasUsed")]
        public string GasUsed { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hasError")]
        public bool? HasError { get; set; }

    }
}
