using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using Nethereum.Hex.HexConvertors.Extensions;
using SigningServiceApiCaller;
using Nethereum.ABI.Util;
using Nethereum.RPC.Eth.TransactionManagers;
using Nethereum.Util;
using Nethereum.Signer;
using SigningServiceApiCaller.Models;
using Core;
using Core.Settings;

namespace LkeServices.Signature
{
    public class LykkeSignedTransactionManager : ITransactionManager
    {
        private static BigInteger DefaultGasPrice = BigInteger.Parse("45000000000");
        private static BigInteger MaxGasPrice = BigInteger.Parse("50000000000");

        private BigInteger _nonceCount = -1;
        private readonly ILykkeSigningAPI _signatureApi;
        private readonly Web3 _web3;
        private readonly IBaseSettings _baseSettings;

        public LykkeSignedTransactionManager(Web3 web3, ILykkeSigningAPI signatureApi, IBaseSettings baseSettings)
        {
            _baseSettings = baseSettings;
            _signatureApi = signatureApi;
            Client = web3.Client;
            _web3 = web3;
        }

        public async Task<HexBigInteger> GetNonceAsync(TransactionInput transaction)
        {
            var ethGetTransactionCount = new EthGetTransactionCount(Client);
            var nonce = transaction.Nonce;
            if (nonce == null)
            {
                nonce = await ethGetTransactionCount.SendRequestAsync(transaction.From).ConfigureAwait(false);

                if (nonce.Value <= _nonceCount)
                {
                    _nonceCount = _nonceCount + 1;
                    nonce = new HexBigInteger(_nonceCount);
                }
                else
                    _nonceCount = nonce.Value;
            }
            return nonce;
        }

        public async Task<string> SendTransactionAsync<T>(T transaction) where T : TransactionInput
        {
            var ethSendTransaction = new EthSendRawTransaction(Client);
            var currentGasPriceHex = await _web3.Eth.GasPrice.SendRequestAsync();
            var currentGasPrice = currentGasPriceHex.Value;
            var nonce = await GetNonceAsync(transaction);
            var value = transaction.Value?.Value ?? 0;
            BigInteger selectedGasPrice = (currentGasPrice > MaxGasPrice) ? MaxGasPrice : currentGasPrice * _baseSettings.GasPricePercentage / 100;
            var gasPrice = transaction.GasPrice?.Value ?? selectedGasPrice;
            var gasValue = transaction.Gas?.Value ?? Constants.GasForCoinTransaction;
            var tr = new Nethereum.Signer.Transaction(transaction.To, value, nonce, gasPrice, gasValue, transaction.Data);
            var hex = tr.GetRLPEncoded().ToHex();

            var requestBody = new EthereumTransactionSignRequest()
            {
                FromProperty = new AddressUtil().ConvertToChecksumAddress(transaction.From),
                Transaction = hex
            };

            var response = await _signatureApi.ApiEthereumSignPostAsync(requestBody);

            return await ethSendTransaction.SendRequestAsync(response.SignedTransaction.EnsureHexPrefix()).ConfigureAwait(false);
        }

        public IClient Client { get; set; }
    }
}
