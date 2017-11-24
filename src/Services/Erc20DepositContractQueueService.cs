using System.Threading.Tasks;
using AzureStorage.Queue;
using Core.Exceptions;
using Core.Notifiers;

namespace Services
{
    public class Erc20DepositContractQueueService : IErc20DepositContractQueueService
    {
        private readonly IQueueExt _queue;
        private readonly ISlackNotifier _slackNotifier;

        public Erc20DepositContractQueueService(
            IQueueExt queue,
            ISlackNotifier slackNotifier)
        {
            _queue = queue;
            _slackNotifier = slackNotifier;
        }

        public async Task<string> GetContractAddress()
        {
            var message = await _queue.GetRawMessageAsync();
            
            if (message != null)
            {
                await _queue.FinishRawMessageAsync(message);

                return message.AsString;
            }
            else
            {
                await _slackNotifier.ErrorAsync("Ethereum Core Service! Erc20 deposit contract pool is empty!");

                throw new ClientSideException(ExceptionType.ContractPoolEmpty, "Erc20 deposit contract pool is empty!");
            }
        }

        public async Task PushContractAddress(string contractAddress)
        {
            await _queue.PutRawMessageAsync(contractAddress);
        }

        public async Task<int> Count()
        {
            return await _queue.Count() ?? 0;
        }
    }
}