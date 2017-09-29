using Core;
using Core.Exceptions;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Services.Signature;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Services.Transactions
{
    public interface ITransactionValidationService
    {
        Task<bool> IsTransactionErc20Transfer(string transactionHex);
        Task ValidateAddressBalanceAsync(string address, BigInteger value, BigInteger gasAmount, BigInteger gasPrice);
        Task ThrowOnExistingHashAsync(string trHash);
        Task ValidateInputForSignedAsync(string fromAddress, string signedTransaction);
    }

    public class TransactionValidationService : ITransactionValidationService
    {
        private readonly IPaymentService _paymentService;
        private readonly ISignatureChecker _signatureChecker;
        private readonly IEthereumTransactionService _ethereumTransactionService;

        public TransactionValidationService(IPaymentService paymentService,
            IEthereumTransactionService ethereumTransactionService,
            ISignatureChecker signatureChecker)
        {
            _ethereumTransactionService = ethereumTransactionService;
            _paymentService = paymentService;
            _signatureChecker = signatureChecker;
        }

        public async Task<bool> IsTransactionErc20Transfer(string transactionHex)
        {
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(transactionHex.HexToByteArray());
            string erc20InvocationData = transaction.Data?.ToHexCompact().EnsureHexPrefix();
            
            return erc20InvocationData?.IndexOf(Constants.Erc20TransferSignature, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async Task ValidateInputForSignedAsync(string fromAddress, string signedTransaction)
        {
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTransaction.HexToByteArray());
            bool isSignedRight = await _signatureChecker.CheckTransactionSign(fromAddress, signedTransaction);
            string valueHex = transaction.Value.ToHex();
            string gasLimit = transaction.GasLimit.ToHex();
            string gasPrice = transaction.GasPrice.ToHex();

            await this.ThrowOnExistingHashAsync(transaction.Hash.ToHex());
            ThrowOnWrongSignature(isSignedRight);
            await ValidateAddressBalanceAsync(fromAddress,
                new HexBigInteger(transaction.Value.ToHex()),
                new HexBigInteger(gasLimit),
                new HexBigInteger(gasPrice));
        }

        public async Task ValidateAddressBalanceAsync(string address, BigInteger value, BigInteger gasAmount, BigInteger gasPrice)
        {
            var balance = await _paymentService.GetAddressBalancePendingInWei(address);
            var transactionCost = value + gasAmount * gasPrice;

            if (balance < transactionCost)
            {
                throw new ClientSideException(ExceptionType.NotEnoughFunds, "Not enough funds");
            }
        }

        public async Task ThrowOnExistingHashAsync(string trHash)
        {
            bool transactionInPool = await _ethereumTransactionService.IsTransactionInPool(trHash);
            TransactionReceipt reciept = await _ethereumTransactionService.GetTransactionReceipt(trHash);

            if (transactionInPool || reciept != null)
            {
                throw new ClientSideException(ExceptionType.TransactionExists, $"Transaction with hash {trHash} already exists");
            }
        }

        private void ThrowOnWrongSignature(bool isSignedRight)
        {
            if (!isSignedRight)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "Wrong Signature");
            }
        }
    }
}
