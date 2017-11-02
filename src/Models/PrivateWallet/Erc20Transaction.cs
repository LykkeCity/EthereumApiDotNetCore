using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BusinessModels.PrivateWallet
{
    public class Erc20Transaction : TransactionBase
    {
        public string TokenAddress { get; set; }
        public BigInteger TokenAmount { get; set; }
    }
}
