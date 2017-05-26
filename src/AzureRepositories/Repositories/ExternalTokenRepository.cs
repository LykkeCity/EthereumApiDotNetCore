using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;

namespace AzureRepositories.Repositories
{
    public class ExternalTokenEntity : TableEntity, IExternalToken
    {
        public string Id { get; set; }
        public string Name { get; set; }
        //key
        public string ContractAddress
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public byte Divisibility { get; set; }
        public string TokenSymbol { get; set; }
        public string Version { get; set; }
        public string InitialSupply { get; set; }

        public static string GeneratePartitionKey()
        {
            return "ExternalToken";
         }
        public static ExternalTokenEntity CreateEntity(IExternalToken token)
        {
            return new ExternalTokenEntity
            {
                PartitionKey = GeneratePartitionKey(),
                Id = token.Id,
                Name = token.Name,
                ContractAddress = token.ContractAddress,
                Divisibility = token.Divisibility,
                InitialSupply = token.InitialSupply,
                TokenSymbol = token.TokenSymbol,
                Version = token.Version
            };
        }
    }

    public class ExternalTokenRepository : IExternalTokenRepository
    {
        private readonly INoSQLTableStorage<ExternalTokenEntity> _table;

        public ExternalTokenRepository(INoSQLTableStorage<ExternalTokenEntity> table)
        {
            _table = table;
        }

        public async Task<IEnumerable<IExternalToken>> GetAllAsync()
        {
            IEnumerable<IExternalToken> tokens = await _table.GetDataAsync(ExternalTokenEntity.GeneratePartitionKey());

            return tokens;
        }

        public async Task<IExternalToken> GetAsync(string externalTokenAddress)
        {
            var token = await _table.GetDataAsync(ExternalTokenEntity.GeneratePartitionKey(), externalTokenAddress);

            return token;
        }

        public async Task SaveAsync(IExternalToken token)
        {
            var entity = ExternalTokenEntity.CreateEntity(token);

            await _table.InsertAsync(entity);
        }
    }
}
