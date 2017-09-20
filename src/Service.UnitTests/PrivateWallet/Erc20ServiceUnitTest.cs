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
using Core.Settings;
using BusinessModels.PrivateWallet;
using Nethereum.Contracts;
using Services.Transactions;

namespace Service.UnitTests.PrivateWallet
{
    [TestClass]
    public class Erc20ServiceUnitTest : BaseTest
    {
        #region ERC20_ABI
        private string _erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_spender\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"name\":\"supply\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"},{\"name\":\"_spender\",\"type\":\"address\"}],\"name\":\"allowance\",\"outputs\":[{\"name\":\"remaining\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"to\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"spender\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"}]";
        #endregion
        private string _contractAddress = "0xaa4981d084120aef4bbaeecb9abdbc7d180c7edb";
        private string _privateKey = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";
        Erc20Service _erc20Service;
        private MockNonceCalculator _nonceCalc;
        private Mock<IClient> _client;
        private ISignatureChecker _signatureChecker;

        [TestInitialize]
        public void TestInit()
        {
            _client = new Mock<IClient>();
            Mock<IWeb3> web3Mock = new Mock<IWeb3>();
            Mock<IBaseSettings> baseSettings = new Mock<IBaseSettings>();
            _signatureChecker = Config.Services.GetService<ISignatureChecker>();
            _nonceCalc = (MockNonceCalculator)Config.Services.GetService<INonceCalculator>();
            #region SetupMock
            _client.Setup(x => x.SendRequestAsync<string>(It.IsAny<Nethereum.JsonRpc.Client.RpcRequest>(), null))
                .Returns(Task.FromResult("")).Verifiable();
            baseSettings.Setup(x => x.ERC20ABI).Returns(_erc20Abi);
            web3Mock.Setup(x => x.Client).Returns(_client.Object);
            web3Mock.Setup(x => x.Eth).Returns(new EthApiContractService(_client.Object));
            #endregion
            IRawTransactionSubmitter rawTransactionSubmitter = new RawTransactionSubmitter(web3Mock.Object, _signatureChecker);
            _erc20Service = new Erc20Service(web3Mock.Object, _nonceCalc, baseSettings.Object, rawTransactionSubmitter, null, null, null);
        }

        [TestMethod]
        public async Task Erc20ServiceUnitTest_TestGetTransaction()
        {
            string from = TestConstants.PW_ADDRESS;
            Erc20Transaction ethTransaction = new Erc20Transaction()
            {
                FromAddress = from,
                GasAmount = 21000,
                GasPrice = 30000000000,
                ToAddress = _contractAddress,
                TokenAddress = _contractAddress,
                TokenAmount = 30000000000
            };

            string transactionHex = await _erc20Service.GetTransferTransactionRaw(ethTransaction);
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(transactionHex.HexToByteArray());

            Assert.AreEqual(_nonceCalc._nonceStorage[from].Value, new HexBigInteger(transaction.Nonce.ToHex()));
            Assert.AreEqual(ethTransaction.GasAmount, new HexBigInteger(transaction.GasLimit.ToHex()));
            Assert.AreEqual(new HexBigInteger(0).Value, new HexBigInteger(transaction.Value.ToHex()));
            Assert.AreEqual(ethTransaction.GasPrice, new HexBigInteger(transaction.GasPrice.ToHex()));
            Assert.AreEqual(ethTransaction.ToAddress.ToLower(), transaction.ReceiveAddress.ToHex().EnsureHexPrefix());
            Assert.IsNotNull(transaction.Data.ToHex().EnsureHexPrefix());
        }

        [TestMethod]
        public async Task Erc20ServiceUnitTest_SignAndCommitTransaction()
        {
            string from = TestConstants.PW_ADDRESS;
            Erc20Transaction ethTransaction = new Erc20Transaction()
            {
                FromAddress = from,
                GasAmount = 21000,
                GasPrice = 30000000000,
                ToAddress = _contractAddress,
                TokenAddress = _contractAddress,
                TokenAmount = 30000000000
            };

            string transactionHex = await _erc20Service.GetTransferTransactionRaw(ethTransaction);
            string signedTransaction = SignRawTransaction(transactionHex, _privateKey);
            string transactionHash = await _erc20Service.SubmitSignedTransaction(from, signedTransaction);
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTransaction.HexToByteArray());

            _client.Verify(x => x.SendRequestAsync<string>(It.IsAny<Nethereum.JsonRpc.Client.RpcRequest>(), null), Times.Once);
            Assert.AreEqual(from, transaction.Key.GetPublicAddress());
            Assert.AreEqual(_nonceCalc._nonceStorage[from].Value, new HexBigInteger(transaction.Nonce.ToHex()));
            Assert.AreEqual(ethTransaction.GasAmount, new HexBigInteger(transaction.GasLimit.ToHex()));
            Assert.AreEqual(new HexBigInteger(0).Value, new HexBigInteger(transaction.Value.ToHex()));
            Assert.AreEqual(ethTransaction.GasPrice, new HexBigInteger(transaction.GasPrice.ToHex()));
            Assert.AreEqual(ethTransaction.ToAddress.ToLower(), transaction.ReceiveAddress.ToHex().EnsureHexPrefix());
        }

        [TestMethod]
        public async Task Erc20ServiceUnitTest_WrongSignAndCommitTransaction()
        {
            bool isExceptionProcessed = false;
            string from = TestConstants.PW_ADDRESS;
            Erc20Transaction ethTransaction = new Erc20Transaction()
            {
                FromAddress = from,
                GasAmount = 21000,
                GasPrice = 30000000000,
                ToAddress = _contractAddress,
                TokenAddress = _contractAddress,
                TokenAmount = 30000000000
            };

            string transactionHex = await _erc20Service.GetTransferTransactionRaw(ethTransaction);
            string wrongKey = _privateKey.Remove(2, 1).Insert(2, "3");
            string signedTransaction = SignRawTransaction(transactionHex, wrongKey);
            try
            {
                string transactionHash = await _erc20Service.SubmitSignedTransaction(from, signedTransaction);
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
