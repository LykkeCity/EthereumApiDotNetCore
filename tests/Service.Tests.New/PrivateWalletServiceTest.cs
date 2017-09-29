using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Tests;
using Microsoft.Extensions.DependencyInjection;
using Services.PrivateWallet;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Diagnostics;
using Nethereum.Signer;
using BusinessModels;
using Nethereum.Web3;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using BusinessModels.PrivateWallet;
using Services.Model;
using Services.Transactions;

namespace Service.Tests
{
    [TestClass]
    public class PrivateWalletServiceTest : BaseTest
    {
        private ITransactionValidationService _transactionValidationService;
        private IPrivateWalletService _privateWallet;
        private string _privateKey = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";
        private Web3 _web3;

        [TestInitialize]
        public void Init()
        {
            _transactionValidationService = Config.Services.GetService<ITransactionValidationService>();
            _privateWallet = Config.Services.GetService<IPrivateWalletService>();
            _web3 = Config.Services.GetService<Web3>();
        }

        [TestMethod]
        public async Task PrivateWalletServiceTest_TestBroadcastingMultiple()
        {
            string fromAddress = "0x46Ea3e8d85A06cBBd8c6a491a09409f5B59BEa28";
            EthTransaction transaction = new EthTransaction()
            {
                FromAddress = fromAddress,
                GasAmount = 21000,
                GasPrice = 50000000000,
                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
                Value = 5000000000
            };

            for (int i = 0; i < 2; i++)
            {
                string trRaw = await _privateWallet.GetTransactionForSigning(transaction);
                string signedRawTr = SignRawTransaction(trRaw, _privateKey);
                string transactionHash;
                if (!await _transactionValidationService.IsTransactionErc20Transfer(signedRawTr))
                {
                    transactionHash = await _privateWallet.SubmitSignedTransaction(fromAddress, signedRawTr);
                }
                else
                {
                    throw new Exception("Can;t reach it");
                }
            }
        }

        [TestMethod]
        public async Task PrivateWalletServiceTest_TestTransactionEstimation()
        {
            string fromAddress = "0x46Ea3e8d85A06cBBd8c6a491a09409f5B59BEa28";
            EthTransaction transaction = new EthTransaction()
            {
                FromAddress = fromAddress,
                GasAmount = 21000,
                GasPrice = 50000000000,
                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",//"0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
                Value = 5000000000
            };

            string trRaw = await _privateWallet.GetTransactionForSigning(transaction);
            string signedRawTr = SignRawTransaction(trRaw, _privateKey);
            OperationEstimationResult result = await _privateWallet.EstimateTransactionExecutionCost(fromAddress, signedRawTr);
        }

        [TestMethod]
        public async Task PrivateWalletServiceTest_GetPendingTransactions()
        {
            var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending());
            foreach (var transaction in block.Transactions)
            {
                Trace.TraceInformation($"from: {transaction.From} - trHash: {transaction.TransactionHash}");
            }
        }


        [TestMethod]
        public async Task SignTransactionOld()
        {
            string trHex = "f86b81978509502f900083030d4094ce2ef46ecc168226f33b6f6b8a56e90450d0d2c080b844a9059cbb000000000000000000000000aa4981d084120aef4bbaeecb9abdbc7d180c7edb000000000000000000000000000000000000000000000000000000000000000a808080";

            string signedHex = SignRawTransaction(trHex, _privateKey);

            Trace.TraceInformation(signedHex);
        }

        private string SignRawTransaction(string trHex, string privateKey)
        {
            var transaction = new Nethereum.Signer.Transaction(trHex.HexToByteArray());
            var secret = new EthECKey(privateKey);
            transaction.Sign(secret);

            string signedHex = transaction.GetRLPEncoded().ToHex();

            return signedHex;
        }
    }
}
