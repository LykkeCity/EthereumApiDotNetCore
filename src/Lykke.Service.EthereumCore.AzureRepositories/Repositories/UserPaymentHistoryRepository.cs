using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class UserPaymentHistoryEntity : TableEntity, IUserPaymentHistory
    {
        public static string GetPartition(string userAddress)
        {
            return $"UserPaymentHistory_{userAddress}";
        }

        public string TransactionHash
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public string Note { get; set; }
        public string UserAddress { get; set; }
        public string ToAddress { get; set; }
        public string Amount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string AdapterAddress { get; set; }


        public static UserPaymentHistoryEntity Create(IUserPaymentHistory userPayment)
        {
            return new UserPaymentHistoryEntity
            {
                PartitionKey = GetPartition(userPayment.UserAddress),
                Amount = userPayment.Amount,
                TransactionHash = userPayment.TransactionHash,
                CreatedDate = userPayment.CreatedDate,
                UserAddress = userPayment.UserAddress,
                ToAddress = userPayment.ToAddress,
                Note = userPayment.Note,
                AdapterAddress = userPayment.AdapterAddress
            };
        }
    }

    public class UserPaymentHistoryRepository : IUserPaymentHistoryRepository
    {
        private readonly INoSQLTableStorage<UserPaymentHistoryEntity> _table;

        public UserPaymentHistoryRepository(INoSQLTableStorage<UserPaymentHistoryEntity> table)
        {
            _table = table;
        }

        public async Task SaveAsync(IUserPaymentHistory transferContract)
        {
            var entity = UserPaymentHistoryEntity.Create(transferContract);

            await _table.InsertAsync(entity);
        }
    }
}
