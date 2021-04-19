﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace Lykke.Service.EthereumCore.Services.New.Models
{
    public class CoinCashinEvent
    {
        [Parameter("address", "caller", 1, false)]
        public string Caller { get; set; }

        [Parameter("uint", "amount", 2, false)]
        public BigInteger Amount { get; set; }

    }
}
