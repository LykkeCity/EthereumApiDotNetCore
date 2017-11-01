using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Job.EthereumCore.Contracts.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CoinEventType
    {
        CashinStarted,
        CashinCompleted,
        CashoutStarted,
        CashoutCompleted,
        TransferStarted,
        TransferCompleted
    }
}
