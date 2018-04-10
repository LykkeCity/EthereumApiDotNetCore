using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Job.EthereumCore.Contracts.Enums.LykkePay
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SenderType
    {
        Customer,
        EthereumCore
    }
}
