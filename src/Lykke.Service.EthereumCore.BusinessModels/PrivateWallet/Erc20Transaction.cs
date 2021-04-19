using System.Numerics;

namespace Lykke.Service.EthereumCore.BusinessModels.PrivateWallet
{
    public class Erc20Transaction : TransactionBase
    {
        public string TokenAddress { get; set; }
        public BigInteger TokenAmount { get; set; }
    }
}
