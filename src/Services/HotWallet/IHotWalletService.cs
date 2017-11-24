using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Services.HotWallet
{
    public interface IHotWalletService
    {
        Task RetryCashoutAsync(IHotWalletOperation hotWalletCashout);
        Task EnqueueCashoutAsync(IHotWalletOperation hotWalletCashout);
        Task<string> StartCashoutAsync(string operationId);
        Task SaveOperationAsync(IHotWalletOperation operation);
        Task<string> StartCashinAsync(IHotWalletOperation operation);
        Task RemoveCashinLockAsync(string erc20TokenAddress, string userAddress);
    }
}
