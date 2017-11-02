using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using System.Threading.Tasks;

namespace Services.Signature
{
    public class NonceCalculator : INonceCalculator
    {
        private readonly EthGetTransactionCount _getTransactionCount;


        public NonceCalculator(Web3 web3)
        {
            _getTransactionCount = new EthGetTransactionCount(web3.Client);
        }

        
        public async Task<HexBigInteger> GetNonceAsync(string fromAddress)
        {
            return await _getTransactionCount.SendRequestAsync(fromAddress, BlockParameter.CreatePending());
        }
    }
}
