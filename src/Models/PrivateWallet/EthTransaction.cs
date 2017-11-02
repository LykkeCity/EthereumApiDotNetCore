using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BusinessModels.PrivateWallet
{
    public class EthTransaction : TransactionBase
    {
        public BigInteger Value { get; set; }
    }
}
