using AzureStorage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Notifiers
{
    public interface ISlackNotifier
    {
        Task WarningAsync(string message);
        Task ErrorAsync(string message);
        Task FinanceWarningAsync(string message);
    }
}
