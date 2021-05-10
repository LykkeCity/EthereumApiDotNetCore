﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Common;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;

namespace Lykke.Service.EthereumCore.Services
{
    public class Erc20DepositContractPoolService : IErc20DepositContractPoolService
    {
        private readonly IErc20DepositContractService _contractService;
        private readonly IErc20DepositContractQueueServiceFactory _poolFactory;
        private readonly IEthereumContractPoolRepository _ethereumContractPoolRepository;
        private readonly IBaseSettings _settings;

        public Erc20DepositContractPoolService(
            [KeyFilter(Constants.DefaultKey)]IErc20DepositContractService contractService,
            IErc20DepositContractQueueServiceFactory poolFactory,
            IEthereumContractPoolRepository ethereumContractPoolRepository,
            IBaseSettings settings)
        {
            _contractService = contractService;
            _poolFactory = poolFactory;
            _ethereumContractPoolRepository = ethereumContractPoolRepository;
            _settings = settings;
        }

        // TODO: CLI To save not assigned contracts! 

        public async Task ReplenishPool()
        {
            var pool = _poolFactory.Get(Constants.Erc20DepositContractPoolQueue);

            var currentCount = await pool.Count();

            var notCompleted = await _ethereumContractPoolRepository.GetAsync();


            IReadOnlyCollection<string> notCompletedHash = Array.Empty<string>();

            if (!string.IsNullOrEmpty(notCompleted?.TxHashes))
                notCompletedHash = Newtonsoft.Json.JsonConvert.DeserializeObject<IReadOnlyCollection<string>>(notCompleted.TxHashes);

            if (currentCount < _settings.MinContractPoolLength)
            {
                while (currentCount < _settings.MaxContractPoolLength)
                {
                    IReadOnlyCollection<string> hashes = null;
                    if (!notCompletedHash.Any())
                    {
                        var tasks = Enumerable
                            .Range(0, _settings.ContractsPerRequest)
                            .Select(x => _contractService.CreateContract());

                        hashes = (await Task.WhenAll(tasks))
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToArray();

                        await _ethereumContractPoolRepository.SaveAsync(new EthereumContractPool()
                        {
                            TxHashes = Newtonsoft.Json.JsonConvert.SerializeObject(hashes)
                        });
                    }
                    else
                    {
                        hashes = notCompletedHash;

                        notCompletedHash = Array.Empty<string>();
                    }

                    //TODO: It is possible that some hashes are declined by the node(rollback) and we must fix here manually
                    var contractAddresses = await _contractService.GetContractAddresses(hashes);

                    var addToPoolTasks = contractAddresses
                        .Select(async contractAddress =>
                            {
                                var isCreated = await _ethereumContractPoolRepository.GetOrDefaultAsync(contractAddress);

                                if (!isCreated)
                                {
                                    //TODO: It is possible to not push contract address in the pool. But CLI will fix it
                                    await _ethereumContractPoolRepository.InsertOrReplaceAsync(contractAddress);
                                    await pool.PushContractAddress(contractAddress);
                                }
                            }
                        );

                    await Task.WhenAll(addToPoolTasks);

                    await _ethereumContractPoolRepository.ClearAsync();

                    currentCount += _settings.ContractsPerRequest;
                }
            }
        }
    }

    /// <summary>
    /// Default and LykkePay
    /// </summary>
    public interface IErc20DepositContractPoolService
    {
        Task ReplenishPool();
    }
}