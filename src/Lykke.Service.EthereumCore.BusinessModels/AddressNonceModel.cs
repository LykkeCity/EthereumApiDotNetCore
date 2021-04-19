using System.Numerics;

namespace Lykke.Service.EthereumCore.BusinessModels
{
    public class AddressNonceModel
    {
        public string Address { get; set; }
        public BigInteger Nonce { get; set; }
    }
}
