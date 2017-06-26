using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BusinessModels
{
    public class EthTransaction
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public BigInteger GasAmount { get; set; }
        public BigInteger GasPrice { get; set; }
        public BigInteger Value { get; set; }
    }
}
