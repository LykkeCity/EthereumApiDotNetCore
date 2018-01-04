using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using System.Numerics;

namespace Lykke.Service.EthereumCore.AzureRepositories.Repositories
{
    public class AddressNonceEntity : TableEntity, IAddressNonce
    {
        public string Address
        {
            get
            {
                return this.RowKey;
            }
            set
            {
                this.RowKey = value;
            }
        }
        public String NonceStr { get; set; }
        public BigInteger Nonce
        {
            get
            {
                return BigInteger.Parse(this.NonceStr);
            }
            set
            {
                this.NonceStr = value.ToString();
            }
        }

        public static string GetPartitionKey()
        {
            return "AddressNonce";
        }

        public static AddressNonceEntity CreateEntity(IAddressNonce addressNonce)
        {
            return new AddressNonceEntity
            {
                PartitionKey = GetPartitionKey(),
                Address = addressNonce.Address,
                Nonce = addressNonce.Nonce,
            };
        }
    }


    public class NonceRepository : INonceRepository
    {
        private readonly INoSQLTableStorage<AddressNonceEntity> _table;

        public NonceRepository(INoSQLTableStorage<AddressNonceEntity> table)
        {
            _table = table;
        }

        public async Task SaveAsync(IAddressNonce nonce)
        {
            var entity = AddressNonceEntity.CreateEntity(nonce);

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task<IAddressNonce> GetAsync(string address)
        {
            var entity = await _table.GetDataAsync(AddressNonceEntity.GetPartitionKey(), address);

            return entity;
        }

        public async Task CleanAsync()
        {
            Func<IEnumerable<IAddressNonce>, Task> func = async (items) =>
            {
                foreach (var item in items)
                {
                    await _table.DeleteIfExistAsync(AddressNonceEntity.GetPartitionKey(), item.Address);
                }
            };

            await _table.GetDataByChunksAsync(func);
        }
    }
}
