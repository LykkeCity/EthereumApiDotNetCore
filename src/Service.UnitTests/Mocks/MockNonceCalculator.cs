using Lykke.Service.EthereumCore.Services.Signature;
using System;
using System.Collections.Generic;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System.Threading.Tasks;

namespace Service.UnitTests.Mocks
{
    public class MockNonceCalculator : INonceCalculator
    {
        public Dictionary<string, HexBigInteger> _nonceStorage = new Dictionary<string, HexBigInteger>();

        public Task<HexBigInteger> GetNonceAsync(TransactionInput transaction)
        {
            throw new NotImplementedException();
        }

        public Task<HexBigInteger> GetNonceAsync(string fromAddress, bool checkTxPool)
        {
            HexBigInteger currentNonce;
            _nonceStorage.TryGetValue(fromAddress, out currentNonce);
            if (currentNonce == null)
            {
                currentNonce = new HexBigInteger(0);
                _nonceStorage[fromAddress] = currentNonce;
            }

            return Task.FromResult(currentNonce);
        }

        public Task<HexBigInteger> GetNonceLatestAsync(string fromAddress)
        {
            throw new NotImplementedException();
        }
    }
}
