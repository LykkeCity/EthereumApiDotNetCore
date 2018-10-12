using Lykke.Service.EthereumCore.BusinessModels;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.BusinessModels.PrivateWallet;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Util;
using Nethereum.Web3;
using Lykke.Service.EthereumCore.Services.Model;
using Lykke.Service.EthereumCore.Services.Signature;
using Lykke.Service.EthereumCore.Services.Transactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Services;

namespace Lykke.Service.EthereumCore.Services.PrivateWallet
{
    public interface IPrivateWalletService
    {
        Task<string> GetDataTransactionForSigning(DataTransaction ethTransaction, bool useTxPool = false);
        Task<OperationEstimationResult> EstimateTransactionExecutionCost(string from, string signedTrHex);
        Task<string> GetTransactionForSigning(EthTransaction ethTransaction, bool useTxPool = false);
        Task<string> SubmitSignedTransaction(string from, string signedTrHex);
        Task<bool> CheckTransactionSign(string from, string signedTrHex);
        Task ValidateInputAsync(TransactionBase transaction);
    }

    public class PrivateWalletService : IPrivateWalletService
    {
        private readonly IWeb3 _web3;
        private readonly INonceCalculator _nonceCalculator;
        private AddressUtil _addressUtil;
        private readonly IEthereumTransactionService _ethereumTransactionService;
        private readonly IPaymentService _paymentService;
        private readonly IRawTransactionSubmitter _rawTransactionSubmitter;
        private readonly ISignatureChecker _signatureChecker;
        private readonly ITransactionValidationService _transactionValidationService;
        private readonly IErc20PrivateWalletService _erc20Service;

        public PrivateWalletService(IWeb3 web3,
            INonceCalculator nonceCalculator,
            IEthereumTransactionService ethereumTransactionService,
            IPaymentService paymentService,
            ISignatureChecker signatureChecker,
            ITransactionValidationService transactionValidationService,
            IErc20PrivateWalletService erc20Service,
            IRawTransactionSubmitter rawTransactionSubmitter)
        {
            _signatureChecker             = signatureChecker;
            _rawTransactionSubmitter      = rawTransactionSubmitter;
            _nonceCalculator              = nonceCalculator;
            _web3                         = web3;
            _ethereumTransactionService   = ethereumTransactionService;
            _paymentService               = paymentService;
            _transactionValidationService = transactionValidationService;
            _erc20Service                 = erc20Service;
        }

        public async Task<string> GetTransactionForSigning(EthTransaction ethTransaction, bool useTxPool = false)
        {
            string from = ethTransaction.FromAddress;

            var gas      = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.GasAmount);
            var gasPrice = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.GasPrice);
            var nonce    = await _nonceCalculator.GetNonceAsync(from, useTxPool);
            var to       = ethTransaction.ToAddress;
            var value    = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.Value);
            var tr       = new Nethereum.Signer.Transaction(to, value, nonce, gasPrice, gas);
            var hex      = tr.GetRLPEncoded().ToHex();

            return hex;
        }

        public async Task<string> GetDataTransactionForSigning(DataTransaction ethTransaction, bool useTxPool = false)
        {
            string from = ethTransaction.FromAddress;

            var gas = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.GasAmount);
            var gasPrice = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.GasPrice);
            var nonce = await _nonceCalculator.GetNonceAsync(from, useTxPool);
            var to = ethTransaction.ToAddress;
            var value = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.Value);
            var data = ethTransaction.Data;
            var tr = new Nethereum.Signer.Transaction(to, value, nonce, gasPrice, gas, data);
            var hex = tr.GetRLPEncoded().ToHex();

            return hex;
        }

        public async Task<OperationEstimationResult> EstimateTransactionExecutionCost(string from, string signedTrHex)
        {
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTrHex.HexToByteArray());
            var increasedGas                         = new HexBigInteger(transaction.GasLimit.ToHexCompact()).Value + 1;
            var gasLimit                             = new HexBigInteger(increasedGas);
            var gasPrice                             = new HexBigInteger(transaction.GasPrice.ToHexCompact());
            string hexValue                          = transaction.Value.ToHexCompact();
            var value                                = new HexBigInteger(!string.IsNullOrEmpty(hexValue) ? hexValue : "0");
            var to                                   = transaction.ReceiveAddress.ToHex().EnsureHexPrefix();
            var data                                 = transaction?.Data?.ToHex()?.EnsureHexPrefix() ?? "";
            var callInput                            = new CallInput(data, to, from, gasLimit, gasPrice, value);
            HexBigInteger response;

            try
            {
                var callResult = await _web3.Eth.Transactions.Call.SendRequestAsync(callInput);
                response = await _web3.Eth.Transactions.EstimateGas.SendRequestAsync(callInput);
            }
            catch (Exception e)
            {
                response = new HexBigInteger(gasLimit.Value);
            }

            return new OperationEstimationResult()
            {
                GasAmount = response.Value,
                IsAllowed = response.Value < gasLimit.Value || response.Value == Constants.DefaultTransactionGas
            };
        }

        public async Task<string> SubmitSignedTransaction(string from, string signedTrHex)
        {
            await _transactionValidationService.ValidateInputForSignedAsync(from, signedTrHex);
            string transactionHex = await _rawTransactionSubmitter.SubmitSignedTransaction(from, signedTrHex);

            return transactionHex;
        }

        public async Task<bool> CheckTransactionSign(string from, string signedTrHex)
        {
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTrHex.HexToByteArray());
            string signedBy = transaction.Key.GetPublicAddress();

            return _addressUtil.ConvertToChecksumAddress(from) == _addressUtil.ConvertToChecksumAddress(signedBy);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        /// <exception cref="ClientSideException">Throws client side exception</exception>
        public async Task ValidateInputAsync(TransactionBase transaction)
        {
            await _transactionValidationService.ValidateAddressBalanceAsync(transaction.FromAddress, transaction.Value, transaction.GasAmount, transaction.GasPrice);
        }
    }
}
