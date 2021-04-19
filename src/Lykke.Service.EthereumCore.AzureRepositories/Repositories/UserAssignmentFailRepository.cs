﻿using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class UserAssignmentFailEntity : TableEntity, IUserAssignmentFail
    {
        public static string GetPartition()
        {
            return "UserAssignmentFail";
        }

        public string ContractAddress
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public int FailCount { get; set; }
        public bool CanBeRestoredInternally { get; set; }
        public bool? NotifiedInSlack { get; set; }

        public static UserAssignmentFailEntity Create(IUserAssignmentFail userAssignmentFail)
        {
            return new UserAssignmentFailEntity
            {
                CanBeRestoredInternally = userAssignmentFail.CanBeRestoredInternally,
                FailCount = userAssignmentFail.FailCount,
                ContractAddress = userAssignmentFail.ContractAddress,
                PartitionKey = GetPartition(),
                NotifiedInSlack = userAssignmentFail.NotifiedInSlack
            };
        }
    }

    public class UserAssignmentFailRepository : IUserAssignmentFailRepository
    {
        private readonly INoSQLTableStorage<UserAssignmentFailEntity> _table;

        public UserAssignmentFailRepository(INoSQLTableStorage<UserAssignmentFailEntity> table)
        {
            _table = table;
        }

        public async Task<IUserAssignmentFail> GetAsync(string contractAddress)
        {
            var result = await _table.GetDataAsync(UserAssignmentFailEntity.GetPartition(), contractAddress);

            return result;
        }

        public async Task SaveAsync(IUserAssignmentFail userAssignmentFail)
        {
            var entity = UserAssignmentFailEntity.Create(userAssignmentFail);

            await _table.InsertOrReplaceAsync(entity);
        }
    }
}
