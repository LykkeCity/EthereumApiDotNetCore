using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using System.Numerics;

namespace AzureRepositories.Repositories
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
        public string ContractAddress { get; set; }
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
                ContractAddress = userPayment.ContractAddress,
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
