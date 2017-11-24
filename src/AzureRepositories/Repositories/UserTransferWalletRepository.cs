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
    public class UserTransferWalletEntity : TableEntity, IUserTransferWallet
    {
        public static string GenerateParitionKey(string userAddress)
        {
            return $"UserTransferWallet_{userAddress}";
        }
        public string UserAddress { get; set; }

        public string TransferContractAddress
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public DateTime UpdateDate { get; set; }

        public string LastBalance { get; set; }

        public static UserTransferWalletEntity Create(IUserTransferWallet userTransferWallet)
        {
            return new UserTransferWalletEntity
            {
                PartitionKey = GenerateParitionKey(userTransferWallet.UserAddress),
                RowKey = userTransferWallet.TransferContractAddress,
                UpdateDate = userTransferWallet.UpdateDate,
                UserAddress = userTransferWallet.UserAddress.ToLower(),
                TransferContractAddress = userTransferWallet.TransferContractAddress,
                LastBalance = userTransferWallet.LastBalance
            };
        }
    }

    public class UserTransferWalletRepository : IUserTransferWalletRepository
    {
        private readonly INoSQLTableStorage<UserTransferWalletEntity> _table;

        public UserTransferWalletRepository(INoSQLTableStorage<UserTransferWalletEntity> table)
        {
            _table = table;
        }

        public async Task DeleteAsync(string userAddress, string transferContractAddress)
        {
            await _table.DeleteIfExistAsync(UserTransferWalletEntity.GenerateParitionKey(userAddress), transferContractAddress);
        }

        public string FormatAddressForErc20(string depositContractAddress, string erc20TokenAddress)
        {
            return $"{erc20TokenAddress}_{depositContractAddress}";
        }

        public async Task<IEnumerable<IUserTransferWallet>> GetAllAsync()
        {
            return await _table.GetDataAsync((x) => true);
        }

        public async Task<IUserTransferWallet> GetUserContractAsync(string userAddress, string transferContractAddress)
        {
            string lowerUserAddress = userAddress.ToLower();
            IUserTransferWallet wallet =
                await _table.GetDataAsync(UserTransferWalletEntity.GenerateParitionKey(lowerUserAddress), transferContractAddress);

            return wallet;
        }

        public async Task ReplaceAsync(IUserTransferWallet wallet)
        {
            var entity = UserTransferWalletEntity.Create(wallet);

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task SaveAsync(IUserTransferWallet wallet)
        {
            var entity = UserTransferWalletEntity.Create(wallet);

            await _table.InsertAsync(entity);
        }
    }
}
