using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Job.EthereumCore.Contracts.Enums.LykkePay
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventType
    {
        Detected = 0,
        Started = 1,
        Completed = 2
    }
}
