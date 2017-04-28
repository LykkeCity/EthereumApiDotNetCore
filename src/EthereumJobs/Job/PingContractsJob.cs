
using System.Threading.Tasks;
using Core.Timers;
using Services.Coins;
using Common.Log;

namespace EthereumJobs.Job
{
	public class PingContractsJob : TimerPeriod
	{
		private readonly ICoinContractService _coinContractService;
		private const int TimerPeriodSeconds = 60 * 60 * 24;

		public PingContractsJob(ICoinContractService coinContractService, ILog log) : base("PingContractsJob", TimerPeriodSeconds * 1000, log)
		{
			_coinContractService = coinContractService;
		}

		public override async Task Execute()
		{
			await _coinContractService.PingMainExchangeContract();
			//TODO : ping coin contracts
		}
	}
}
