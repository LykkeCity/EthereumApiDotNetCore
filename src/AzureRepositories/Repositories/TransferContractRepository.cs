using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;

namespace AzureRepositories.Repositories
{
    public class TransferContractEntity : TableEntity, ITransferContract
    {
        public static string GenerateParitionKey()
        {
            return "TransferContract";
        }
        public string ContractAddress
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public string UserAddress { get; set; }

        public string CoinAdapterAddress { get; set; }

        public string ExternalTokenAddress { get; set; }

        public bool ContainsEth { get; set; }

        public static TransferContractEntity Create(ITransferContract transferContract)
        {
            return new TransferContractEntity
            {
                PartitionKey = GenerateParitionKey(),
                CoinAdapterAddress = transferContract.CoinAdapterAddress,
                ContainsEth = transferContract.ContainsEth,
                UserAddress = transferContract.UserAddress,
                ContractAddress = transferContract.ContractAddress,
                ExternalTokenAddress = transferContract.ExternalTokenAddress,
            };
        }
    }

    public class TransferContractRepository : ITransferContractRepository
    {
        private readonly INoSQLTableStorage<TransferContractEntity> _table;

        public TransferContractRepository(INoSQLTableStorage<TransferContractEntity> table)
        {
            _table = table;
        }

        public async Task<ITransferContract> GetAsync(string contractAddress)
        {
            ITransferContract result = await _table.GetDataAsync(TransferContractEntity.GenerateParitionKey(), contractAddress);

            return result;
        }

        public async Task ProcessAllAsync(Func<ITransferContract, Task> processAction)
        {
            await _table.GetDataByChunksAsync(TransferContractEntity.GenerateParitionKey(), async (items) => 
            {
                foreach (var item in items)
                {
                    await processAction(item);
                }
            });
        }

        public async Task SaveAsync(ITransferContract transferContract)
        {
            var entity = TransferContractEntity.Create(transferContract);

            await _table.InsertAsync(entity);
        }
    }
}
