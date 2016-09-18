using System;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Exceptions;

namespace Services
{
	public interface IContractQueueService
	{
		Task<string> GetContract();
		Task PushContract(string contract);
		Task<int> Count();
	}

	public class ContractQueueService : IContractQueueService
	{
		private readonly IEmailNotifierService _emailNotifier;
		private readonly IQueueExt _queue;

		public ContractQueueService(Func<string, IQueueExt> queueFactory, IEmailNotifierService emailNotifier)
		{
			_emailNotifier = emailNotifier;
			_queue = queueFactory(Constants.EthereumContractQueue);
		}

		public async Task<string> GetContract()
		{
			var message = await _queue.GetRawMessageAsync();
			if (message == null)
				return null;

			await _queue.FinishRawMessageAsync(message);

			var contract = message.AsString;

			if (string.IsNullOrWhiteSpace(contract))
			{
				_emailNotifier.Warning("Ethereum", "User contract pool is empty!");
				throw new BackendException(BackendExceptionType.ContractPoolEmpty);
			}

			return contract;
		}

		public async Task PushContract(string contract)
		{
			await _queue.PutRawMessageAsync(contract);
		}

		public async Task<int> Count()
		{
			return await _queue.Count() ?? 0;
		}
	}
}
