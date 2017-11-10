using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Services.HotWallet
{
    public interface IHotWalletService
    {
        Task EnqueueCashoutAsync(IHotWalletCashout hotWalletCashout);
        Task<string> StartCashoutAsync(string operationId);
    }
}
