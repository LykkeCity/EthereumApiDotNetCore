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
using Nethereum.Util;
using Nethereum.Signer;
using SigningServiceApiCaller.Models;
using Core;
using Core.Settings;
using System.Threading;
using Services.Signature;
using Nethereum.RPC.TransactionManagers;
using System;

namespace LkeServices.Signature
{
    public class LykkeSignedTransactionManager : ITransactionManager
    {
        private static BigInteger _minGasPrice;
        private static BigInteger _maxGasPrice;
        private BigInteger _nonceCount = -1;
        private readonly ILykkeSigningAPI _signatureApi;
        private readonly Web3 _web3;
        private readonly IBaseSettings _baseSettings;
        private readonly SemaphoreSlim _readLock;
        private readonly INonceCalculator _nonceCalculator;

        public IClient Client { get; set; }
        public BigInteger DefaultGasPrice { get; set; }
        public BigInteger DefaultGas { get; set; }

        public LykkeSignedTransactionManager(Web3 web3, ILykkeSigningAPI signatureApi, IBaseSettings baseSettings, INonceCalculator nonceCalculator)
        {
            _nonceCalculator = nonceCalculator;
            _baseSettings = baseSettings;
            _maxGasPrice = new BigInteger(_baseSettings.MaxGasPrice);
            _minGasPrice = new BigInteger(_baseSettings.MinGasPrice);
            _signatureApi = signatureApi;
            Client = web3.Client;
            _web3 = web3;
            _readLock = new SemaphoreSlim(1, 1);
        }

        public async Task<HexBigInteger> GetNonceAsync(TransactionInput transaction)
        {
            var ethGetTransactionCount = new EthGetTransactionCount(Client);
            var nonce = transaction.Nonce;
            if (nonce == null)
            {
                nonce = await GetNonceAsync(transaction.From).ConfigureAwait(false);
            }

            return nonce;
        }

        public async Task<HexBigInteger> GetNonceAsync(string fromAddress)
        {
            var ethGetTransactionCount = new EthGetTransactionCount(Client);
            var nonce = await ethGetTransactionCount.SendRequestAsync(fromAddress).ConfigureAwait(false);

            if (nonce.Value <= _nonceCount)
            {
                _nonceCount = _nonceCount + 1;
                nonce = new HexBigInteger(_nonceCount);
            }
            else
            {
                _nonceCount = nonce.Value;
            }

            return nonce;
        }

        public async Task<string> SendTransactionAsync<T>(T transaction) where T : TransactionInput
        {
            var value = (transaction?.Value ?? new BigInteger(0));
            return await SendTransactionASync(transaction.From, transaction.To, 
                transaction.Data,
                value,
                transaction.GasPrice,
                transaction.Gas);
        }

        public Task<HexBigInteger> EstimateGasAsync<T>(T callInput) where T : CallInput
        {
            throw new NotImplementedException();
        }

        public async Task<string> SendTransactionAsync(string from, string to, HexBigInteger amount)
        {
            return await SendTransactionASync(from, to, "", amount);
        }

        private async Task<string> SendTransactionASync(string from, string to, string data, BigInteger value, BigInteger? gasPrice = null, BigInteger? gasValue = null)
        {
            var ethSendTransaction = new EthSendRawTransaction(Client);
            var currentGasPriceHex = await _web3.Eth.GasPrice.SendRequestAsync();
            var currentGasPrice = currentGasPriceHex.Value;
            HexBigInteger nonce;
            try
            {
                await _readLock.WaitAsync();
                nonce = await GetNonceAsync(from);
            }
            finally
            {
                _readLock.Release();
            }
            
            BigInteger selectedGasPrice = currentGasPrice * _baseSettings.GasPricePercentage / 100;
            if (selectedGasPrice > _maxGasPrice)
            {
                selectedGasPrice = _maxGasPrice;
            }
            else if (selectedGasPrice < _minGasPrice)
            {
                selectedGasPrice = _minGasPrice;
            }

            gasPrice = gasPrice == null || gasPrice.Value == 0 ? selectedGasPrice : gasPrice;
            gasValue = gasValue == null || gasValue.Value == 0 ? Constants.GasForCoinTransaction : gasValue;
            var tr = new Nethereum.Signer.Transaction(to, value, nonce.Value, gasPrice.Value, gasValue.Value, data);
            var hex = tr.GetRLPEncoded().ToHex();

            var requestBody = new EthereumTransactionSignRequest()
            {
                FromProperty = new AddressUtil().ConvertToChecksumAddress(from),
                Transaction = hex
            };

            var response = await _signatureApi.ApiEthereumSignPostAsync(requestBody);

            return await ethSendTransaction.SendRequestAsync(response.SignedTransaction.EnsureHexPrefix()).ConfigureAwait(false);
        }

        public async Task<HexBigInteger> EstimateGasAsync<T>(T callInput) where T : CallInput
        {
            if (Client == null) throw new NullReferenceException("Client not configured");
            if (callInput == null) throw new ArgumentNullException(nameof(callInput));
            var ethEstimateGas = new EthEstimateGas(Client);
            return await ethEstimateGas.SendRequestAsync(callInput);
        }

        public async Task<string> SendTransactionAsync(string from, string to, HexBigInteger amount)
        {
            return await SendTransactionAsync(new TransactionInput("", to, from, new HexBigInteger(DefaultGas), amount));
        }
    }
}
