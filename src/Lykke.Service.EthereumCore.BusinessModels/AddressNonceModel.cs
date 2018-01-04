using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lykke.Service.EthereumCore.BusinessModels
{
    public class AddressNonceModel
    {
        public string Address { get; set; }
        public BigInteger Nonce { get; set; }
    }
}
