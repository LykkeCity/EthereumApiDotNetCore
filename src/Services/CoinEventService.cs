using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services.Coins;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services
{
    public interface ICoinEventService
    {
        Task<ICoinEvent> GetCoinEvent(string transactionHash);
        Task<ICoinEvent> GetCoinEventById(string OperationId);
        Task PublishEvent(ICoinEvent coinEvent, bool putInProcessingQueue = true);
        Task InsertAsync(ICoinEvent coinEvent);
    }

    public class CoinEventService : ICoinEventService
    {
        private readonly ICoinEventPublisher _coinEventPublisher;
        private readonly ICoinEventRepository _coinEventRepository;
        private readonly ICoinTransactionService _coinTransactionService;

        public CoinEventService(ICoinEventPublisher coinEventPublisher,
            ICoinEventRepository coinEventRepository,
            ICoinTransactionService coinTransactionService)
        {
            _coinEventPublisher = coinEventPublisher;
            _coinEventRepository = coinEventRepository;
            _coinTransactionService = coinTransactionService;
        }

        public async Task<ICoinEvent> GetCoinEvent(string transactionHash)
        {
            var coinEvent = await _coinEventRepository.GetCoinEvent(transactionHash);

            return coinEvent;
        }

        public async Task<ICoinEvent> GetCoinEventById(string operationId)
        {
            var coinEvent = await _coinEventRepository.GetCoinEventById(operationId);

            return coinEvent;
        }

        public async Task InsertAsync(ICoinEvent coinEvent)
        {
            await _coinEventRepository.InsertOrReplace(coinEvent);
        }

        public async Task PublishEvent(ICoinEvent coinEvent, bool putInProcessingQueue = true)
        {
            await _coinEventRepository.InsertOrReplace(coinEvent);
            await _coinEventPublisher.PublishEvent(coinEvent);

            if (putInProcessingQueue)
            {
                await _coinTransactionService.PutTransactionToQueue(coinEvent.TransactionHash, coinEvent.OperationId);
            }
        }
    }
}
