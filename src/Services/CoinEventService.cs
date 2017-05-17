using Core.Repositories;
using Services.Coins;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface ICoinEventService
    {
        Task<ICoinEvent> GetCoinEvent(string transactionHash);
        Task PublishEvent(ICoinEvent coinEvent, bool putInProcessingQueue = true);
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

        public async Task PublishEvent(ICoinEvent coinEvent, bool putInProcessingQueue = true)
        {
            await _coinEventRepository.InsertOrReplace(coinEvent);
            await _coinEventPublisher.PublishEvent(coinEvent);

            if (putInProcessingQueue)
            {
                await _coinTransactionService.PutTransactionToQueue(coinEvent.TransactionHash);
            }
        }
    }
}
