using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
	public interface IUserContract
	{
		string Address { get; }
		DateTime CreateDt { get; }
	}

	public class UserContract : IUserContract
	{
		public string Address { get; set; }
		public DateTime CreateDt { get; set; }
	}

	public interface IUserContractRepository
	{
		Task AddAsync(IUserContract contract);
		Task<IEnumerable<IUserContract>> GetContractsAsync();
	}
}
