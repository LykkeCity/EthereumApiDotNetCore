using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using System.Numerics;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class UserPaymentRepository : IUserPaymentRepository
    {
        public async Task SaveAsync(IUserPayment transferContract)
        {
        }
    }
}
