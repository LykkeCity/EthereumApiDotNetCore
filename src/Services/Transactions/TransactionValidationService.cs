using Core.Exceptions;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Services.Transactions
{
    public interface ITransactionValidationService
    {
        Task ValidateAddressBalanceAsync(string address, BigInteger value, BigInteger gasAmount, BigInteger gasPrice);
        Task ThrowOnExistingHashAsync(string trHash);
    }

    public class TransactionValidationService : ITransactionValidationService
    {
        private readonly IPaymentService _paymentService;
        private readonly EthereumTransactionService _ethereumTransactionService;

        public TransactionValidationService(IPaymentService paymentService, EthereumTransactionService ethereumTransactionService)
        {
            _ethereumTransactionService = ethereumTransactionService;
            _paymentService = paymentService;
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
    }
}
