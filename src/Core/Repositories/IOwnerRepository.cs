using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IOwner
    {
        string Address { get; set; }
    }

    public class Owner : IOwner
    {
        public string Address { get; set; }
    }

    public interface IOwnerRepository
    {
        Task<IEnumerable<IOwner>> GetAllAsync();
        Task SaveAsync(IOwner owner);
        Task RemoveAsync(string address);
    }
}
