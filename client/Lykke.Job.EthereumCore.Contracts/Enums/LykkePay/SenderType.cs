﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Job.EthereumCore.Contracts.Enums.LykkePay
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SenderType
    {
        Customer = 0,
        EthereumCore = 1
    }
}
