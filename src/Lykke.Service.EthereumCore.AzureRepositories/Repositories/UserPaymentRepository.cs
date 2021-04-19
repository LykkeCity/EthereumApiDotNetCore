using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class UserPaymentRepository : IUserPaymentRepository
    {
        public async Task SaveAsync(IUserPayment transferContract)
        {
        }
    }
}
