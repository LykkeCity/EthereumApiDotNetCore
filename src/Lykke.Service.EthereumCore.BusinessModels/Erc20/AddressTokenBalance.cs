using System.Numerics;

namespace Lykke.Service.EthereumCore.BusinessModels.Erc20
{
    public class AddressTokenBalance
    {
        public BigInteger Balance { get; set; }

        public string Erc20TokenAddress { get; set; }

        public string UserAddress { get; set; }
    }
}