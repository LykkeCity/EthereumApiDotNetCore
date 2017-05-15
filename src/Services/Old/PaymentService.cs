using System;
using System.Numerics;
using System.Threading.Tasks;
using Core;
using Core.ContractEvents;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Common.Log;
using Nethereum.Util;

namespace Services
{
    public interface IPaymentService
    {
        Task<decimal> GetMainAccountBalance();

        Task<decimal> GetUserContractBalance(string address);

        //Task<bool> ProcessPaymentEvent(UserPaymentEvent log);
        Task<BigInteger> GetTransferContractBalanceInWei(string transferContractAddress);

        //Task<bool> ProcessPaymentEvent(UserPaymentEvent log);
        Task<string> SendEthereum(string fromAddress, string toAddress, BigInteger amont);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IBaseSettings _settings;
        private readonly ILog _logger;
        private readonly Web3 _web3;

        //private readonly IContractTransferTransactionService _contractTransferTransactionService;

        public PaymentService(IBaseSettings settings, ILog logger, Web3 web3)
        {
            _web3 = web3;
            _settings = settings;
            _logger = logger;
        }

        public async Task<decimal> GetMainAccountBalance()
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(_settings.EthereumMainAccount);

            return UnitConversion.Convert.FromWei(balance);
        }

        public async Task<decimal> GetUserContractBalance(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);

            return UnitConversion.Convert.FromWei(balance);
        }

        public async Task<BigInteger> GetTransferContractBalanceInWei(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);

            return balance.Value;
        }

        public async Task<string> SendEthereum(string fromAddress, string toAddress, BigInteger amount)
        {
            string transactionHash = await _web3.Eth.Transactions.SendTransaction.SendRequestAsync(
                new Nethereum.RPC.Eth.DTOs.TransactionInput("", toAddress, fromAddress, new HexBigInteger(Constants.GasForEthCashin), new HexBigInteger(amount)));

            return transactionHash;
        }

        //public async Task<bool> ProcessPaymentEvent(UserPaymentEvent log)
        //{
        //    try
        //    {
        //        await _logger.WriteInfoAsync("EthereumJob", "ProcessPaymentEvent", "", $"Start proces: event from {log.Address} for {log.Amount} WEI.");

        //        var transaction = await TransferFromUserContract(log.Address, log.Amount);

        //        await _logger.WriteInfoAsync("EthereumJob", "ProcessPaymentEvent", "", $"Finish process: Event from {log.Address} for {log.Amount} WEI. Transaction: {transaction}");

        //        await _contractTransferTransactionService.PutContractTransferTransaction(new ContractTransferTransaction
        //        {
        //            TransactionHash = transaction,
        //            Contract = log.Address,
        //            Amount = UnitConversion.Convert.FromWei(log.Amount),
        //            CreateDt = DateTime.UtcNow
        //        });

        //        await _logger.WriteInfoAsync("EthereumJob", "ProcessPaymentEvent", "", $"Message sended to queue: Event from {log.Address}. Transaction: {transaction}");

        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        await _logger.WriteErrorAsync("EthereumJob", "ProcessPaymentEvent", "Failed to process item", e);
        //    }

        //    return false;
        //}
    }
}
