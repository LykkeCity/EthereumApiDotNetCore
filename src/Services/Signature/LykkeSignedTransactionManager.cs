using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Accounts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.RPC.TransactionManagers;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.Util;
using Nethereum.Web3;
using SigningServiceApiCaller;
using SigningServiceApiCaller.Models;

namespace Lykke.Service.EthereumCore.Services.Signature
{
    public class LykkeSignedTransactionManager : ITransactionManager
    {
        private readonly IBaseSettings _baseSettings;
        private readonly EthEstimateGas _estimateGas;
        private readonly INonceCalculator _nonceCalculator;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores;
        private readonly EthSendRawTransaction _sendRawTransaction;
        private readonly ILykkeSigningAPI _signingApi;
        private readonly ITransactionRouter _transactionRouter;
        private readonly Web3 _web3;
        private readonly IGasPriceRepository _gasPriceRepository;
        private readonly IOverrideNonceRepository _overrideNonceRepository;


        public LykkeSignedTransactionManager(
            IBaseSettings baseSettings,
            INonceCalculator nonceCalculator,
            ILykkeSigningAPI signingApi,
            ITransactionRouter transactionRouter,
            Web3 web3,
            IGasPriceRepository gasPriceRepository,
            IOverrideNonceRepository overrideNonceRepository)
        {
            _baseSettings = baseSettings;
            _estimateGas = new EthEstimateGas(web3.Client);
            _nonceCalculator = nonceCalculator;
            _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _sendRawTransaction = new EthSendRawTransaction(web3.Client);
            _signingApi = signingApi;
            _transactionRouter = transactionRouter;
            _web3 = web3;
            _gasPriceRepository = gasPriceRepository;
            _overrideNonceRepository = overrideNonceRepository;

            Client = web3.Client;
        }


        public Task<TransactionReceipt> SendTransactionAndWaitForReceiptAsync(TransactionInput transactionInput, CancellationTokenSource tokenSource)
        {
            throw new NotImplementedException();
        }

        public IClient Client { get; set; }

        public BigInteger DefaultGasPrice { get; set; }

        public BigInteger DefaultGas { get; set; }

        public IAccount Account => throw new NotImplementedException();

        private ITransactionReceiptService _transactionReceiptService;
        public ITransactionReceiptService TransactionReceiptService
        {
            get
            {
                if (_transactionReceiptService == null)
                    return TransactionReceiptServiceFactory.GetDefaultransactionReceiptService(this);
                return _transactionReceiptService;
            }
            set
            {
                _transactionReceiptService = value;
            }
        }

        IAccount ITransactionManager.Account => throw new NotImplementedException();


        public async Task<string> SendTransactionAsync(TransactionInput transactionInput)
        {
            if (transactionInput == null)
            {
                throw new ArgumentNullException();
            }

            return await SendTransactionAsync
            (
                transactionInput.From,
                transactionInput.To,
                transactionInput.Data,
                transactionInput.Value ?? new BigInteger(0),
                transactionInput.GasPrice ?? new BigInteger(0),
                transactionInput.Gas ?? new BigInteger(0)
            );
        }

        public async Task<HexBigInteger> EstimateGasAsync(CallInput callInput)
        {
            if (Client == null)
            {
                throw new NullReferenceException("Client not configured");
            }

            if (callInput == null)
            {
                throw new ArgumentNullException(nameof(callInput));
            }

            var (gasPrice, gasValue) = await GetGasPriceAndValueAsync(callInput.GasPrice ?? BigInteger.Zero, callInput.Gas ?? BigInteger.Zero);

            callInput.Gas = new HexBigInteger(gasValue.Value);
            callInput.GasPrice = new HexBigInteger(gasPrice.Value);

            return await _estimateGas.SendRequestAsync(callInput);
        }

        public async Task<string> SendTransactionAsync(string from, string to, HexBigInteger amount)
        {
            return await SendTransactionAsync
            (
                from,
                to,
                string.Empty,
                amount,
                null,
                null
            );
        }

        public Task<string> SignTransactionAsync(TransactionInput transaction)
        {
            throw new NotImplementedException();
        }

        private async Task<string> SendTransactionAsync(string from, string to, string data, BigInteger value,
            BigInteger? gasPrice, BigInteger? gasValue)
        {
            from = from == Constants.AddressForRoundRobinTransactionSending
                 ? await _transactionRouter.GetNextSenderAddressAsync()
                 : from;

            var semaphore = _semaphores.GetOrAdd(from, f => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync();

                (gasPrice, gasValue) = await GetGasPriceAndValueAsync(gasPrice, gasValue);

                var nonceStuck = await _overrideNonceRepository.GetNonceAsync(from);
                HexBigInteger nonce;

                if (!string.IsNullOrEmpty(nonceStuck) && BigInteger.TryParse(nonceStuck, out var nonceBig))
                {
                    nonce = await _nonceCalculator.GetNonceLatestAsync(from);
                }
                else
                {
                    nonce = await _nonceCalculator.GetNonceAsync(from, true);
                }

                var transaction = new Nethereum.Signer.TransactionChainId(to, value, nonce.Value, gasPrice.Value, gasValue.Value, data,
                    _baseSettings.ChainId);
                var signRequest = new EthereumTransactionSignRequest
                {
                    FromProperty = new AddressUtil().ConvertToChecksumAddress(from),
                    Transaction = transaction.GetRLPEncoded().ToHex()
                };

                var signResponse = await _signingApi.ApiEthereumSignPostAsync(signRequest);
                var txHash = await _sendRawTransaction.SendRequestAsync(signResponse.SignedTransaction.EnsureHexPrefix());

                return txHash;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<(BigInteger? gasPrice, BigInteger? gasValue)> GetGasPriceAndValueAsync(BigInteger? gasPrice, BigInteger? gasValue)
        {
            var gasPriceSetting = await _gasPriceRepository.GetAsync();
            var currentGasPrice = (await _web3.Eth.GasPrice.SendRequestAsync()).Value;
            var selectedGasPrice = currentGasPrice * _baseSettings.GasPricePercentage / 100;


            if (selectedGasPrice > gasPriceSetting.Max)
            {
                selectedGasPrice = gasPriceSetting.Max;
            }
            else if (selectedGasPrice < gasPriceSetting.Min)
            {
                selectedGasPrice = gasPriceSetting.Min;
            }


            gasPrice = selectedGasPrice;
            gasValue = gasValue == null || gasValue.Value == 0 || gasValue.Value == 21000 ? Constants.GasForCoinTransaction : gasValue;

            return (gasPrice, gasValue);
        }
    }
}