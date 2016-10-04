using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{

	public interface ICoinContractFilter
	{
		string EventName { get; }
		string ContractAddress { get; set; }
		string Filter { get; }
	}

	public class CoinContractFilter : ICoinContractFilter
	{
		public string EventName { get; set; }
		public string ContractAddress { get; set; }
		public string Filter { get; set; }
	}


	public interface ICoinContractFilterRepository
	{
		Task AddFilterAsync(ICoinContractFilter filter);
		Task<IEnumerable<ICoinContractFilter>> GetListAsync();
		Task Clear();
	}
}
