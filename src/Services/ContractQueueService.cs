using System;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Exceptions;
using Core.Log;
using Core.Repositories;

namespace Services
{
	public interface IContractQueueService
	{
		Task<string> GetContract();
		Task<string> GetContractAndSave();
		Task PushContract(string contract);
		Task<int> Count();
	}

	public class ContractQueueService : IContractQueueService
	{
		private readonly IEmailNotifierService _emailNotifier;
		private readonly IUserContractRepository _userContractRepository;
		private readonly ILog _logger;
		private readonly IQueueExt _queue;

		public ContractQueueService(Func<string, IQueueExt> queueFactory, IEmailNotifierService emailNotifier, IUserContractRepository userContractRepository, ILog logger)
		{
			_emailNotifier = emailNotifier;
			_userContractRepository = userContractRepository;
			_logger = logger;
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

		public async Task<string> GetContractAndSave()
		{
			var contract = await GetContract();

			await SaveContractAsync(contract);

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

		private async Task SaveContractAsync(string contract)
		{
			try
			{
				await _userContractRepository.AddAsync(new UserContract { Address = contract, CreateDt = DateTime.UtcNow });
			}
			catch (Exception e)
			{
				await _logger.WriteError("ContractQueueService", "SaveContractAsync", contract, e);
			}
		}
	}
}
