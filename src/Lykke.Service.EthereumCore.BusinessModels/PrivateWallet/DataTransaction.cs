using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lykke.Service.EthereumCore.BusinessModels.PrivateWallet
{
    public class DataTransaction : TransactionBase
    {
        public string Data { get; set; }
    }
}
