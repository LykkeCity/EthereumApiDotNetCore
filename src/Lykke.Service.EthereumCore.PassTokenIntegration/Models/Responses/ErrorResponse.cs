using System.Runtime.Serialization;

namespace Lykke.Service.EthereumCore.PassTokenIntegration.Models.Responses
{
    [DataContract]
    public class BlockPassErrorResponse
    {
        [DataMember(Name = "msg")]
        public string Message { get; set; }
    }
}

/*
    {
        "err": 403, // --___--
        "msg": "Missing api key"
    }
 */
