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

namespace Service.Tests
{
    [TestClass]
    public class Erc20ServiceTest : BaseTest
    {
        private IErc20PrivateWalletService _erc20Service;
        private string _privateKey = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";
        private Web3 _web3;

        [TestInitialize]
        public void Init()
        {
            _erc20Service = Config.Services.GetService<IErc20PrivateWalletService>();
            _web3 = Config.Services.GetService<Web3>();
        }

        [TestMethod]
        public async Task PrivateWalletServiceTest_TestBroadcastingMultiple()
        {
            string fromAddress = "0x46Ea3e8d85A06cBBd8c6a491a09409f5B59BEa28";
            Erc20Transaction transaction = new Erc20Transaction()
            {
                FromAddress = fromAddress,
                GasAmount = 200000,
                GasPrice = 30000000000,
                ToAddress = "0xaA4981d084120AEf4BbaEeCB9abdBc7D180C7EdB",
                TokenAddress = "0xce2ef46ecc168226f33b6f6b8a56e90450d0d2c0",
                TokenAmount = 10
            };

            string trRaw = await _erc20Service.GetTransferTransactionRaw(transaction);
            string signedRawTr = SignRawTransaction(trRaw, _privateKey);
            string trHash1 = await _erc20Service.SubmitSignedTransaction(fromAddress, signedRawTr);
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
