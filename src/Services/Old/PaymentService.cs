using System.Numerics;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Common.Log;
using Lykke.Common.Log;
using Nethereum.Util;
using Nethereum.RPC.Eth.DTOs;

namespace Lykke.Service.EthereumCore.Services
{
    public interface IPaymentService
    {
        Task<decimal> GetMainAccountBalance();

        Task<decimal> GetUserContractBalance(string address);

        Task<BigInteger> GetAddressBalanceInWei(string address);

        Task<BigInteger> GetAddressBalancePendingInWei(string address);

        Task<string> SendEthereum(string fromAddress, string toAddress, BigInteger amont);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IBaseSettings _settings;
        private readonly ILog _logger;
        private readonly Web3 _web3;

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

        public async Task<BigInteger> GetAddressBalanceInWei(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);

            return balance.Value;
        }

        public async Task<BigInteger> GetAddressBalancePendingInWei(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address, BlockParameter.CreatePending());

            return balance.Value;
        }

        public async Task<string> SendEthereum(string fromAddress, string toAddress, BigInteger amount)
        {
            _logger.Info("Sending ethereum", new
            {
                GasAmount = _settings.GasForEthCashin,
                ToAddress = toAddress,
                FromAddress = fromAddress,
                Amount = amount
            });
            
            string transactionHash = await _web3.Eth.Transactions.SendTransaction.SendRequestAsync(
                new TransactionInput("",
                    toAddress,
                    fromAddress,
                 new HexBigInteger(_settings.GasForEthCashin),
                new HexBigInteger(amount)));

            return transactionHash;
        }
    }
}
