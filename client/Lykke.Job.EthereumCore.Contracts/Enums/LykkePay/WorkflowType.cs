using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Job.EthereumCore.Contracts.Enums.LykkePay
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WorkflowType
    {
        LykkePay = 0,
        Airlines = 1
    }
}
