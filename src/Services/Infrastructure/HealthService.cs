using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Domain.Health;
using Lykke.Service.EthereumCore.Core.Services;
using Nethereum.Web3;

namespace Lykke.Service.EthereumCore.Services
{
    public class HealthService : IHealthService
    {
        private readonly Web3 _web3;
        private readonly IContractService _contractService;

        public HealthService(IContractService contractService, Web3 web3)
        {
            _web3 = web3;
            _contractService = contractService;
        }

        public async  Task<string> GetHealthViolationMessage()
        {
            // TODO: Check gathered health statistics, and return appropriate health violation message, or NULL if service hasn't critical errors
            return null;
        }

        public async Task<IEnumerable<HealthIssue>> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            var delayTime = 10;//seconds
            bool isNodeAvailable = false;
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(delayTime));
            var currentBlockTask = _contractService.GetCurrentBlock();
            var currentGasPriceHexTask = _web3.Eth.GasPrice.SendRequestAsync();
            StringBuilder errorMsg = new StringBuilder();

            try
            {
                await Task.WhenAny(new Task[] {
                currentBlockTask,
                currentGasPriceHexTask,
                Task.Delay(TimeSpan.FromSeconds(delayTime + 1), cts.Token)
                });

                isNodeAvailable = true;
            }
            catch (AggregateException e)
            {
                foreach (var item in e.InnerExceptions)
                {
                    errorMsg.AppendLine($"{item.Message}");
                }
            }
            catch (TaskCanceledException e)
            {
                errorMsg.AppendLine($"Timeout happened within {delayTime} seconds");
            }
            catch (Exception e)
            {
                errorMsg.AppendLine(e.Message);
            }

            var block = isNodeAvailable ? await currentBlockTask : System.Numerics.BigInteger.Zero;
            var currentGasPriceHex = isNodeAvailable ? await currentGasPriceHexTask :
                new Nethereum.Hex.HexTypes.HexBigInteger(System.Numerics.BigInteger.Zero);

            issues.Add("BlockNumber", block.ToString());
            issues.Add("Version", Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion);
            issues.Add("CurrentGasPrice", currentGasPriceHex.Value.ToString());
            issues.Add("IsNodeAvailable", isNodeAvailable.ToString());
            issues.Add("NodeIssues", errorMsg.ToString());

            return issues;
        }
    }
}