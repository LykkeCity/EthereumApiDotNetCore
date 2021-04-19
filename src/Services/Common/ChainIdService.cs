using System;
using System.Collections.Generic;
using System.Numerics;

namespace Lykke.Service.EthereumCore.Services.Common
{
    public class ChainIdService
    {
        public BigInteger ChainId { get; }

        public ChainIdService(BigInteger chainId)
        {
            ChainId = chainId;
        }
    }
}
