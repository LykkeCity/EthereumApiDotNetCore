// Code generated by Microsoft (R) AutoRest Code Generator 1.0.1.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace SigningServiceApiCaller.Models
{
    using Newtonsoft.Json;

    public partial class EthereumHashSignRequest
    {
        /// <summary>
        /// Initializes a new instance of the EthereumHashSignRequest class.
        /// </summary>
        public EthereumHashSignRequest()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the EthereumHashSignRequest class.
        /// </summary>
        public EthereumHashSignRequest(string fromProperty = default(string), string hash = default(string))
        {
            FromProperty = fromProperty;
            Hash = hash;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "from")]
        public string FromProperty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

    }
}
