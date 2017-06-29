using BusinessModels;
using Core.Exceptions;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Util;
using Nethereum.Web3;
using Services.Signature;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Services.PrivateWallet
{
    public interface IPrivateWalletService
    {
        Task<string> GetTransactionForSigning(EthTransaction ethTransaction);
        Task<string> SubmitSignedTransaction(string from, string signedTrHex);
        Task<bool> CheckTransactionSign(string from, string signedTrHex);
    }

    public class PrivateWalletService : IPrivateWalletService
    {
        private readonly IWeb3 _web3;
        private readonly INonceCalculator _nonceCalculator;
        private AddressUtil _addressUtil;

        public PrivateWalletService(IWeb3 web3, INonceCalculator nonceCalculator)
        {
            _addressUtil = new AddressUtil();
            _nonceCalculator = nonceCalculator;
            _web3 = web3;
        }

        public async Task<string> GetTransactionForSigning(EthTransaction ethTransaction)
        {
            string from = ethTransaction.FromAddress;

            var gas = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.GasAmount);
            var gasPrice = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.GasPrice);
            var nonce = await _nonceCalculator.GetNonceAsync(from);
            var to = ethTransaction.ToAddress;
            var value = new Nethereum.Hex.HexTypes.HexBigInteger(ethTransaction.Value);
            var tr = new Nethereum.Signer.Transaction(to, value, nonce, gasPrice, gas);
            var hex = tr.GetRLPEncoded().ToHex();

            return hex;
        }

        public async Task<string> SubmitSignedTransaction(string from, string signedTrHex)
        {
            bool isSignedRight = await CheckTransactionSign(from, signedTrHex);
            if (!isSignedRight)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "WrongSign");
            }

            var ethSendTransaction = new EthSendRawTransaction(_web3.Client);
            string transactionHex = await ethSendTransaction.SendRequestAsync(signedTrHex);

            return transactionHex;
        }

        public async Task<bool> CheckTransactionSign(string from, string signedTrHex)
        {
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTrHex.HexToByteArray());
            string signedBy = transaction.Key.GetPublicAddress();

            return _addressUtil.ConvertToChecksumAddress(from) == _addressUtil.ConvertToChecksumAddress(signedBy);
        }
    }
}
