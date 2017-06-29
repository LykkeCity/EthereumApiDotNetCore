using Microsoft.VisualStudio.TestTools.UnitTesting;
using Services.PrivateWallet;
using Services.Signature;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Services;
using Moq;
using Nethereum.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using BusinessModels;
using Nethereum.Hex.HexConvertors.Extensions;
using Service.UnitTests.Mocks;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using Core.Exceptions;

namespace Service.UnitTests.PrivateWallet
{
    [TestClass]
    public class PrivateWalletServiceUnitTest : BaseTest
    {
        private string _privateKey = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";
        PrivateWalletService _privateWalletService;
        private MockNonceCalculator _nonceCalc;
        private Mock<IClient> _client;

        [TestInitialize]
        public void TestInit()
        {
            _client = new Mock<IClient>();
            Mock<IWeb3> web3Mock = new Mock<IWeb3>();
            _nonceCalc = (MockNonceCalculator)Config.Services.GetService<INonceCalculator>();
            #region SetupMockWeb3
            _client.Setup(x => x.SendRequestAsync(It.IsAny<RpcRequest>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new RpcResponse(null, (JToken)null)));
            web3Mock.Setup(x => x.Client).Returns(_client.Object);

            #endregion

            _privateWalletService = new PrivateWalletService(web3Mock.Object, _nonceCalc);
        }

        [TestMethod]
        public async Task PrivateWalletServiceUnitest_TestGetTransaction()
        {
            string from = TestConstants.PW_ADDRESS;
            EthTransaction ethTransaction = new EthTransaction()
            {
                FromAddress = from,
                GasAmount = 21000,
                GasPrice = 30000000000,
                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
                Value = 30000000000
            };

            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(transactionHex.HexToByteArray());

            Assert.AreEqual(_nonceCalc._nonceStorage[from].Value, new HexBigInteger(transaction.Nonce.ToHex()));
            Assert.AreEqual(ethTransaction.GasAmount, new HexBigInteger(transaction.GasLimit.ToHex()));
            Assert.AreEqual(ethTransaction.Value, new HexBigInteger(transaction.Value.ToHex()));
            Assert.AreEqual(ethTransaction.GasPrice, new HexBigInteger(transaction.GasPrice.ToHex()));
            Assert.AreEqual(ethTransaction.ToAddress.ToLower(), transaction.ReceiveAddress.ToHex().EnsureHexPrefix());
        }

        [TestMethod]
        public async Task PrivateWalletServiceUnitest_SignAndCommitTransaction()
        {
            string from = TestConstants.PW_ADDRESS;
            EthTransaction ethTransaction = new EthTransaction()
            {
                FromAddress = from,
                GasAmount = 21000,
                GasPrice = 30000000000,
                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
                Value = 30000000000
            };

            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
            string signedTransaction = SignRawTransaction(transactionHex, _privateKey);
            string transactionHash = await _privateWalletService.SubmitSignedTransaction(from, signedTransaction);
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTransaction.HexToByteArray());

            _client.Verify(x => x.SendRequestAsync(It.IsAny<RpcRequest>(), It.IsAny<string>()), Times.Once);
            Assert.AreEqual(from, transaction.Key.GetPublicAddress());
            Assert.AreEqual(_nonceCalc._nonceStorage[from].Value, new HexBigInteger(transaction.Nonce.ToHex()));
            Assert.AreEqual(ethTransaction.GasAmount, new HexBigInteger(transaction.GasLimit.ToHex()));
            Assert.AreEqual(ethTransaction.Value, new HexBigInteger(transaction.Value.ToHex()));
            Assert.AreEqual(ethTransaction.GasPrice, new HexBigInteger(transaction.GasPrice.ToHex()));
            Assert.AreEqual(ethTransaction.ToAddress.ToLower(), transaction.ReceiveAddress.ToHex().EnsureHexPrefix());
        }

        [TestMethod]
        public async Task PrivateWalletServiceUnitest_WrongSignAndCommitTransaction()
        {
            string from = TestConstants.PW_ADDRESS;
            EthTransaction ethTransaction = new EthTransaction()
            {
                FromAddress = from,
                GasAmount = 21000,
                GasPrice = 30000000000,
                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
                Value = 30000000000
            };

            bool isExceptionProcessed = false;
            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
            string wrongKey = _privateKey.Remove(2, 1).Insert(2, "3");
            string signedTransaction = SignRawTransaction(transactionHex, wrongKey);
            try
            {
                string transactionHash = await _privateWalletService.SubmitSignedTransaction(from, signedTransaction);
            }
            catch (ClientSideException exc) when (exc.ExceptionType == ExceptionType.WrongSign)
            {
                isExceptionProcessed = true;
            }

            Assert.IsTrue(isExceptionProcessed);
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
