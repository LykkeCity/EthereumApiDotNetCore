// Code generated by Microsoft (R) AutoRest Code Generator 1.0.1.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace SigningServiceApiCaller.Models
{
    using Newtonsoft.Json;

    public partial class AddKeyRequest
    {
        /// <summary>
        /// Initializes a new instance of the AddKeyRequest class.
        /// </summary>
        public AddKeyRequest()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the AddKeyRequest class.
        /// </summary>
        public AddKeyRequest(string key = default(string))
        {
            Key = key;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }

    }
}
