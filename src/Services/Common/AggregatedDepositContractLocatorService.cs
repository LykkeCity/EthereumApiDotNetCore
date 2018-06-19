using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Common;

namespace Lykke.Service.EthereumCore.Services.Common
{
    public class AggregatedDepositContractLocatorService : IErc20DepositContractLocatorService
    {
        private readonly IErc20DepositContractLocatorService _lykkePayLocator;
        private readonly IErc20DepositContractLocatorService _airLinesLocator;

        public AggregatedDepositContractLocatorService(
            [KeyFilter(Constants.LykkePayKey)] IErc20DepositContractLocatorService lykkePayLocator,
            [KeyFilter(Constants.AirLinesKey)] IErc20DepositContractLocatorService airLinesLocator)
        {
            _lykkePayLocator = lykkePayLocator;
            _airLinesLocator = airLinesLocator;
        }

        public async Task<bool> ContainsAsync(string address)
        {
            var result = await _lykkePayLocator.ContainsAsync(address) ||
                         await _lykkePayLocator.ContainsAsync(address);

            return result;
        }
    }
}
