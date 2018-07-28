using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DepositContractResolver.Commands
{
    public interface ICommand
    {
        Task<int> ExecuteAsync();
    }
}
