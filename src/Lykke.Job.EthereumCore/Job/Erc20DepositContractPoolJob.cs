using Autofac.Features.AttributeFilters;
using Common;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Services;
using System;
using System.Threading.Tasks;

namespace Lykke.Job.EthereumCore.Job
{
    public class Erc20DepositContractPoolJob
    {
        private readonly IErc20DepositContractPoolService _contractPoolService;
        private readonly IErc20DepositContractPoolService _contractPoolServiceLykkePay;
        private readonly ILog _logger;
        private readonly IErc20DepositContractPoolService _contractPoolServiceAirlines;

        public Erc20DepositContractPoolJob(
            [KeyFilter(Constants.DefaultKey)]IErc20DepositContractPoolService contractPoolService,
            [KeyFilter(Constants.LykkePayKey)]IErc20DepositContractPoolService contractPoolServiceLykkePay,
            [KeyFilter(Constants.AirLinesKey)]IErc20DepositContractPoolService contractPoolServiceAirlines,
            ILog logger)
        {
            _contractPoolService = contractPoolService;
            _contractPoolServiceLykkePay = contractPoolServiceLykkePay;
            _contractPoolServiceAirlines = contractPoolServiceAirlines;
            _logger = logger;
        }

        [TimerTrigger("0.00:01:00")]
        public async Task Execute()
        {
            try
            {
                await _contractPoolService.ReplenishPool();
                await _logger.WriteInfoAsync(nameof(Erc20DepositContractPoolJob), nameof(Execute), "", "Pool have been replenished");
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositContractPoolJob), nameof(Execute), "", e);
            }
        }

        [TimerTrigger("0.00:01:00")]
        public async Task ExecuteForLykkeApi()
        {
            try
            {
                await _contractPoolServiceLykkePay.ReplenishPool();
                await _logger.WriteInfoAsync(nameof(Erc20DepositContractPoolJob), nameof(ExecuteForLykkeApi), "",
                    "Pool have been replenished", DateTime.UtcNow);
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException exc)
            {
                _logger.WriteInfo( nameof(ExecuteForLykkeApi), exc.ToJson(), $"Can't create contracts: {exc?.RpcError.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositContractPoolJob), nameof(ExecuteForLykkeApi), "", e);
            }
        }

        [TimerTrigger("0.00:01:00")]
        public async Task ExecuteForAirlines()
        {
            try
            {
                await _contractPoolServiceAirlines.ReplenishPool();
                await _logger.WriteInfoAsync(nameof(Erc20DepositContractPoolJob), nameof(ExecuteForAirlines), "", "Pool have been replenished", DateTime.UtcNow);
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException exc)
            {
                _logger.WriteInfo(nameof(ExecuteForAirlines), exc.ToJson(), $"Can't create contracts: {exc?.RpcError.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositContractPoolJob), nameof(ExecuteForAirlines), "", e);
            }
        }
    }
}