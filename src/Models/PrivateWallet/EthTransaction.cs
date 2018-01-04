using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lykke.Service.EthereumCore.BusinessModels.PrivateWallet
{
    public class EthTransaction : TransactionBase
    {
        public BigInteger Value { get; set; }
    }
}
