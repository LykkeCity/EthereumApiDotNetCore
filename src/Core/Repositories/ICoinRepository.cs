using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{

	public interface ICoin
	{
		string Blockchain { get; }
		string Name { get; }
		string Address { get; }
		string Multiplier { get; }
        bool Payable { get; }
	}

	public class Coin : ICoin
	{
		public string Blockchain { get; set; }
		public string Name { get; set; }
		public string Address { get; set; }
		public string Multiplier { get; set; }
	    public bool Payable { get; set;  }
	}

    public interface ICoinRepository
    {
	    Task<ICoin> GetCoin(string name);
        Task InsertOrReplace(ICoin coin);
        Task<ICoin> GetCoinByAddress(string coinAddress);
    }
}
