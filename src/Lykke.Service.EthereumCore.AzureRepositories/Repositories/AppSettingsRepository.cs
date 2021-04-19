﻿using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class AppSettingEntity : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "AppSetting";
        }

        public string Value { get; set; }
    }

    public class AppSettingsRepository : IAppSettingsRepository
    {
        private readonly INoSQLTableStorage<AppSettingEntity> _table;

        public AppSettingsRepository(INoSQLTableStorage<AppSettingEntity> table)
        {
            _table = table;
        }

        public async Task SetSettingAsync(string key, string value)
        {
            await _table.InsertOrMergeAsync(new AppSettingEntity
            {
                PartitionKey = AppSettingEntity.GeneratePartitionKey(),
                RowKey = key,
                Value = value
            });
        }

        public async Task<string> GetSettingAsync(string key)
        {
            var entity = await _table.GetDataAsync(AppSettingEntity.GeneratePartitionKey(), key);
            return entity?.Value;
        }

        public void DeleteTable()
        {
            //TODO: Fix
            //_table.d().Wait();
        }
    }
}
