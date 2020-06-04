using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class NonceEntity : TableEntity
    {
        public string Nonce { get; set; }
    }

    public class OverrideNonceRepository : IOverrideNonceRepository
    {
        private readonly INoSQLTableStorage<NonceEntity> _table;

        public OverrideNonceRepository(INoSQLTableStorage<NonceEntity> table)
        {
            _table = table;
        }

        public Task AddAsync(string address, string nonce)
        {
            var entity = new NonceEntity
            {
                PartitionKey = GetPk(),
                RowKey = GetRk(address),
                Nonce = nonce
            };

            return _table.InsertOrReplaceAsync(entity);
        }

        public async Task<Dictionary<string, string>> GetAllAsync()
        {
            var entities = await _table.GetDataAsync(GetPk());
            return entities.ToDictionary(x => x.RowKey, x => x.Nonce);
        }

        public async Task<string> GetNonceAsync(string address)
        {
            var entity = await _table.GetDataAsync(GetPk(), GetRk(address));
            return entity?.Nonce;
        }

        public Task RemoveAsync(string address)
        {
            return _table.DeleteIfExistAsync(GetPk(), GetRk(address));
        }

        private static string GetPk() => "Nonce";
        private static string GetRk(string address) => address.ToLowerInvariant();
    }
}
