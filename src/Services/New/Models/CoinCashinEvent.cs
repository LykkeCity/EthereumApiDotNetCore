using Nethereum.ABI.FunctionEncoding.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Services.New.Models
{
    public class CoinCashinEvent
    {
        [Parameter("address", "caller", 1, false)]
        public string Caller { get; set; }

        [Parameter("uint", "amount", 2, false)]
        public BigInteger Amount { get; set; }

    }
}
