//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Services.PrivateWallet;
//using Services.Signature;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using Microsoft.Extensions.DependencyInjection;
//using Services;
//using Moq;
//using Nethereum.JsonRpc.Client;
//using EdjCase.JsonRpc.Core;
//using Newtonsoft.Json.Linq;
//using System.Threading.Tasks;
//using BusinessModels;
//using Nethereum.Hex.HexConvertors.Extensions;
//using Service.UnitTests.Mocks;
//using Nethereum.Hex.HexTypes;
//using Nethereum.Signer;
//using Core.Exceptions;
//using BusinessModels.PrivateWallet;
//using System.Diagnostics;
//using Nethereum.RPC.Eth;
//using System.Numerics;
//using Nethereum.RPC.Eth.DTOs;
//using Services.Transactions;

//namespace Service.UnitTests.PrivateWallet
//{
//    [TestClass]
//    public class PrivateWalletServiceUnitTest : BaseTest
//    {
//        private string _privateKey = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";
//        PrivateWalletService _privateWalletService;
//        private MockNonceCalculator _nonceCalc;
//        private Mock<IClient> _client;
//        private Mock<IPaymentService> _paymentServiceMock;
//        private Mock<IWeb3> _web3Mock;
//        private Mock<IEthereumTransactionService> _ethereumTransactionServiceMock;
//        private RawTransactionSubmitter _rawTransactionSubmitter;
//        private ITransactionValidationService _transactionValidationService;
//        private ISignatureChecker _signatureChecker;

//        [TestInitialize]
//        public void TestInit()
//        {
//            _client = new Mock<IClient>();
//            _web3Mock = new Mock<IWeb3>();
//            _paymentServiceMock = new Mock<IPaymentService>();
//            _ethereumTransactionServiceMock = new Mock<IEthereumTransactionService>();
//            _nonceCalc = (MockNonceCalculator)Config.Services.GetService<INonceCalculator>();
//            _signatureChecker = Config.Services.GetService<ISignatureChecker>();
//            _transactionValidationService = Config.Services.GetService<ITransactionValidationService>();
//            #region SetupMockWeb3
//            _client.Setup(x => x.SendRequestAsync<string>(It.IsAny<Nethereum.JsonRpc.Client.RpcRequest>(), It.IsAny<string>()))
//                .Returns(Task.FromResult(""));
//            _web3Mock.Setup(x => x.Client).Returns(_client.Object);

//            //Task<T> SendRequestAsync<T>(RpcRequest request, string route = null);
//            //Task<T> SendRequestAsync<T>(string method, string route = null, params object[] paramList);
//            //Task SendRequestAsync(RpcRequest request, string route = null);
//            //Task SendRequestAsync(string method, string route = null, params object[] paramList);
//            _client.Setup(x => x.SendRequestAsync(It.IsAny<Nethereum.JsonRpc.Client.RpcRequest>(), It.IsAny<string>()))
//                .Returns(Task.FromResult(0));
//            _client.Setup(x => x.SendRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
//                .Returns(Task.FromResult(0));
//            _client.Setup(x => x.SendRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
//                .Returns(Task.FromResult<RpcResponse>(new RpcResponse(null, (JToken)null)));
//            _client.Setup(x => x.SendRequestAsync(It.IsAny<Nethereum.JsonRpc.Client.RpcRequest>(), It.IsAny<string>()))
//                .Returns(Task.FromResult<RpcResponse>(new RpcResponse(null, (JToken)null)));
//            _web3Mock.Setup(x => x.Client).Returns(_client.Object);
//            _web3Mock.Setup(x => x.Eth).Returns(new Nethereum.Contracts.EthApiContractService(_client.Object));
//            _paymentServiceMock.Setup(x => x.GetAddressBalancePendingInWei(TestConstants.PW_ADDRESS))
//                .Returns(Task.FromResult(new BigInteger(700000000000000)));

//            #endregion

//            _privateWalletService = new PrivateWalletService(_web3Mock.Object, 
//                _nonceCalc, 
//                _ethereumTransactionServiceMock.Object,
//                _paymentServiceMock.Object,
//                _signatureChecker,
//                _transactionValidationService,
//                null,
//                null);
//        }

//        [TestMethod]
//        public async Task PrivateWalletServiceUnitTest_TestGetTransaction()
//        {
//            string from = TestConstants.PW_ADDRESS;
//            EthTransaction ethTransaction = new EthTransaction()
//            {
//                FromAddress = from,
//                GasAmount = 21000,
//                GasPrice = 30000000000,
//                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
//                Value = 30000000000
//            };

//            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
//            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(transactionHex.HexToByteArray());

//            Assert.AreEqual(_nonceCalc._nonceStorage[from].Value, new HexBigInteger(transaction.Nonce.ToHex()));
//            Assert.AreEqual(ethTransaction.GasAmount, new HexBigInteger(transaction.GasLimit.ToHex()));
//            Assert.AreEqual(ethTransaction.Value, new HexBigInteger(transaction.Value.ToHex()));
//            Assert.AreEqual(ethTransaction.GasPrice, new HexBigInteger(transaction.GasPrice.ToHex()));
//            Assert.AreEqual(ethTransaction.ToAddress.ToLower(), transaction.ReceiveAddress.ToHex().EnsureHexPrefix());
//        }

//        [TestMethod]
//        public async Task PrivateWalletServiceUnitTest_SignAndCommitTransaction()
//        {
//            string from = TestConstants.PW_ADDRESS;
//            EthTransaction ethTransaction = new EthTransaction()
//            {
//                FromAddress = from,
//                GasAmount = 21000,
//                GasPrice = 30000000000,
//                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
//                Value = 30000000000
//            };

//            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
//            string signedTransaction = SignRawTransaction(transactionHex, _privateKey);
//            string transactionHash = await _privateWalletService.SubmitSignedTransaction(from, signedTransaction);
//            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTransaction.HexToByteArray());

//            _client.Verify(x => x.SendRequestAsync(It.IsAny<Nethereum.JsonRpc.Client.RpcRequest>(), It.IsAny<string>()), Times.Once);
//            Assert.AreEqual(from, transaction.Key.GetPublicAddress());
//            Assert.AreEqual(_nonceCalc._nonceStorage[from].Value, new HexBigInteger(transaction.Nonce.ToHex()));
//            Assert.AreEqual(ethTransaction.GasAmount, new HexBigInteger(transaction.GasLimit.ToHex()));
//            Assert.AreEqual(ethTransaction.Value, new HexBigInteger(transaction.Value.ToHex()));
//            Assert.AreEqual(ethTransaction.GasPrice, new HexBigInteger(transaction.GasPrice.ToHex()));
//            Assert.AreEqual(ethTransaction.ToAddress.ToLower(), transaction.ReceiveAddress.ToHex().EnsureHexPrefix());
//        }

//        [TestMethod]
//        public async Task PrivateWalletServiceUnitTest_WrongSignAndCommitTransaction()
//        {
//            string from = TestConstants.PW_ADDRESS;
//            EthTransaction ethTransaction = new EthTransaction()
//            {
//                FromAddress = from,
//                GasAmount = 21000,
//                GasPrice = 30000000000,
//                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
//                Value = 30000000000
//            };

//            bool isExceptionProcessed = false;
//            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
//            string wrongKey = _privateKey.Remove(2, 1).Insert(2, "3");
//            string signedTransaction = SignRawTransaction(transactionHex, wrongKey);
//            try
//            {
//                string transactionHash = await _privateWalletService.SubmitSignedTransaction(from, signedTransaction);
//            }
//            catch (ClientSideException exc) when (exc.ExceptionType == ExceptionType.WrongSign)
//            {
//                isExceptionProcessed = true;
//            }

//            Assert.IsTrue(isExceptionProcessed);
//        }

//        [TestMethod]
//        public async Task PrivateWalletServiceUnitTest_NotEnoughFundsAndCommitTransaction()
//        {
//            #region MockSetup

//            _ethereumTransactionServiceMock = new Mock<IEthereumTransactionService>();
//            _ethereumTransactionServiceMock.Setup(x => x.GetTransactionReceipt(It.IsAny<string>()))
//                .Returns(Task.FromResult(new TransactionReceipt()));
//            _ethereumTransactionServiceMock.Setup(x => x.IsTransactionInPool(It.IsAny<string>()))
//                .Returns(Task.FromResult(true));
//            _privateWalletService = new PrivateWalletService(_web3Mock.Object, _nonceCalc,
//                _rawTransactionSubmitter,
//                _ethereumTransactionServiceMock.Object,
//                _paymentServiceMock.Object,
//                _signatureChecker,
//                _transactionValidationService,
//                null);

//            #endregion

//            string from = TestConstants.PW_ADDRESS;
//            EthTransaction ethTransaction = new EthTransaction()
//            {
//                FromAddress = from,
//                GasAmount = 21000,
//                GasPrice = 30000000000,
//                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
//                Value = 30000000000
//            };

//            bool isExceptionProcessed = false;
//            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
//            string signedTransaction = SignRawTransaction(transactionHex, _privateKey);
//            try
//            {
//                string transactionHash = await _privateWalletService.SubmitSignedTransaction(from, signedTransaction);
//            }
//            catch (ClientSideException exc) when (exc.ExceptionType == ExceptionType.TransactionExists)
//            {
//                isExceptionProcessed = true;
//            }

//            Assert.IsTrue(isExceptionProcessed);
//        }

//        [TestMethod]
//        public async Task PrivateWalletServiceUnitTestNotEnoughFundsAndCommitTransaction()
//        {
//            #region MockSetup

//            _paymentServiceMock = new Mock<IPaymentService>();
//            _paymentServiceMock.Setup(x => x.GetAddressBalancePendingInWei(TestConstants.PW_ADDRESS))
//                .Returns(Task.FromResult<BigInteger>(new BigInteger(100000000000000)));
//            _privateWalletService = new PrivateWalletService(_web3Mock.Object, _nonceCalc,
//                _rawTransactionSubmitter,
//                _ethereumTransactionServiceMock.Object,
//                _paymentServiceMock.Object,
//                _signatureChecker,
//                _transactionValidationService,
//                null);

//            #endregion

//            string from = TestConstants.PW_ADDRESS;
//            EthTransaction ethTransaction = new EthTransaction()
//            {
//                FromAddress = from,
//                GasAmount = 21000,
//                GasPrice = 30000000000,
//                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
//                Value = 30000000000
//            };

//            bool isExceptionProcessed = false;
//            string transactionHex = await _privateWalletService.GetTransactionForSigning(ethTransaction);
//            string signedTransaction = SignRawTransaction(transactionHex, _privateKey);
//            try
//            {
//                string transactionHash = await _privateWalletService.SubmitSignedTransaction(from, signedTransaction);
//            }
//            catch (ClientSideException exc) when (exc.ExceptionType == ExceptionType.NotEnoughFunds)
//            {
//                isExceptionProcessed = true;
//            }

//            Assert.IsTrue(isExceptionProcessed);
//        }

//        [TestMethod]
//        public async Task PrivateWalletServiceUnitTest_SignTransaction()
//        {
//            string trHex = "e581958506fc23ac0082520894fe2b80f7aa6c3d9b4fafeb57d0c9d98005d0e4b60280808080";
//            string from = TestConstants.PW_ADDRESS;
//            string signedTransaction = SignRawTransaction(trHex, _privateKey);

//            Trace.TraceInformation(signedTransaction);
//        }

//        private string SignRawTransaction(string trHex, string privateKey)
//        {
//            var transaction = new Nethereum.Signer.Transaction(trHex.HexToByteArray());
//            var secret = new EthECKey(privateKey);
//            transaction.Sign(secret);

//            string signedHex = transaction.GetRLPEncoded().ToHex();

//            return signedHex;
//        }
//    }
//}
