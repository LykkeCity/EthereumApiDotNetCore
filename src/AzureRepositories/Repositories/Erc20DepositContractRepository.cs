using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repositories
{
    public class Erc20DepositContractRepository : IErc20DepositContractRepository
    {
        private readonly INoSQLTableStorage<Erc20DepositContractEntity> _table;


        public Erc20DepositContractRepository(INoSQLTableStorage<Erc20DepositContractEntity>table)
        {
            _table = table;
        }


        public async Task AddOrReplace(string contractAddress, string userAddress)
        {
            await _table.InsertOrReplaceAsync(new Erc20DepositContractEntity
            {
                ContractAddress = contractAddress,
                PartitionKey = GetParitionKey(),
                RowKey = GetRowKey(userAddress),
                UserAddress = userAddress,
            });
        }

        public async Task<string> Get(string userAddress)
        {
            return (await _table.GetDataAsync(GetParitionKey(), GetRowKey(userAddress)))?
                .ContractAddress;
        }

        public async Task<IEnumerable<string>> GetAll()
        {
            return (await _table.GetDataAsync(GetParitionKey()))
                .Select(x => x.ContractAddress);
        }

        private static string GetParitionKey()
            => "Erc20DepositContract";

        private static string GetRowKey(string userAddress)
            => userAddress;
    }

    public class Erc20DepositContractEntity : TableEntity
    {
        public string ContractAddress { get; set; }

        public string UserAddress { get; set; }
    }
}