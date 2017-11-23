using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Job.EthereumCore.Contracts.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HotWalletEventType
    {
        CashinCompleted,
        CashoutCompleted
    }
}
