using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashinReportGenerator
{
    public class BcnCredentialsRecordEntity : TableEntity, IBcnCredentialsRecord
    {
        public static class ByClientId
        {
            public static string GeneratePartition(string clientId)
            {
                return clientId;
            }

            public static string GenerateRowKey(string assetId)
            {
                return assetId;
            }

            public static BcnCredentialsRecordEntity Create(IBcnCredentialsRecord record)
            {
                return new BcnCredentialsRecordEntity
                {
                    Address = record.Address,
                    AssetAddress = record.AssetAddress,
                    AssetId = record.AssetId,
                    ClientId = record.ClientId,
                    EncodedKey = record.EncodedKey,
                    PublicKey = record.PublicKey,
                    PartitionKey = GeneratePartition(record.ClientId),
                    RowKey = GenerateRowKey(record.AssetId)
                };
            }
        }


        public static class ByAssetAddress
        {
            public static string GeneratePartition()
            {
                return "ByAssetAddress";
            }

            public static string GenerateRowKey(string assetAddress)
            {
                return assetAddress;
            }

            public static BcnCredentialsRecordEntity Create(IBcnCredentialsRecord record)
            {
                return new BcnCredentialsRecordEntity
                {
                    Address = record.Address,
                    AssetAddress = record.AssetAddress,
                    AssetId = record.AssetId,
                    ClientId = record.ClientId,
                    EncodedKey = record.EncodedKey,
                    PublicKey = record.PublicKey,
                    PartitionKey = GeneratePartition(),
                    RowKey = GenerateRowKey(record.AssetAddress)
                };
            }
        }


        public string Address { get; set; }
        public string EncodedKey { get; set; }
        public string PublicKey { get; set; }
        public string ClientId { get; set; }
        public string AssetAddress { get; set; }
        public string AssetId { get; set; }
    }

    public class BcnClientCredentialsRepository : IBcnClientCredentialsRepository
    {
        private readonly INoSQLTableStorage<BcnCredentialsRecordEntity> _tableStorage;

        public BcnClientCredentialsRepository(INoSQLTableStorage<BcnCredentialsRecordEntity> _tableStorage)
        {
            this._tableStorage = _tableStorage;
        }

        public async Task SaveAsync(IBcnCredentialsRecord credsRecord)
        {
            var byClientEntity = BcnCredentialsRecordEntity.ByClientId.Create(credsRecord);
            var byAssetAddressEntity = BcnCredentialsRecordEntity.ByAssetAddress.Create(credsRecord);

            await _tableStorage.InsertAsync(byClientEntity);
            await _tableStorage.InsertAsync(byAssetAddressEntity);
        }

        public async Task<IBcnCredentialsRecord> GetAsync(string clientId, string assetId)
        {
            return await _tableStorage.GetDataAsync(BcnCredentialsRecordEntity.ByClientId.GeneratePartition(clientId),
                BcnCredentialsRecordEntity.ByClientId.GenerateRowKey(assetId));
        }

        public async Task<IEnumerable<IBcnCredentialsRecord>> GetAsync(string clientId)
        {
            return await _tableStorage.GetDataAsync(BcnCredentialsRecordEntity.ByClientId.GeneratePartition(clientId));
        }

        public async Task<string> GetClientAddress(string clientId)
        {
            return (await _tableStorage.GetTopRecordAsync(BcnCredentialsRecordEntity.ByClientId.GeneratePartition(clientId))).Address;
        }

        public async Task<IBcnCredentialsRecord> GetByAssetAddressAsync(string assetAddress)
        {
            return await _tableStorage.GetDataAsync(BcnCredentialsRecordEntity.ByAssetAddress.GeneratePartition(),
                BcnCredentialsRecordEntity.ByAssetAddress.GenerateRowKey(assetAddress));
        }

        public async Task ProcessAllAsync(Func<IBcnCredentialsRecord, Task> processAction)
        {
            await _tableStorage.GetDataByChunksAsync(async (items) =>
            {
                foreach (var item in items)
                {
                    try
                    {
                        await processAction(item);
                    }
                    catch
                    {
                        Console.WriteLine("Error while processing");
                    }
                }
            });
        }
    }

    public interface IBcnCredentialsRecord
    {
        string Address { get; set; }
        string EncodedKey { get; set; }
        string PublicKey { get; set; }
        string AssetId { get; set; }
        string ClientId { get; set; }
        string AssetAddress { get; set; }
    }

    public class BcnCredentialsRecord : IBcnCredentialsRecord
    {
        public string Address { get; set; }
        public string EncodedKey { get; set; }
        public string PublicKey { get; set; }
        public string ClientId { get; set; }
        public string AssetAddress { get; set; }
        public string AssetId { get; set; }
    }

    public interface IBcnClientCredentialsRepository
    {
        Task SaveAsync(IBcnCredentialsRecord credsRecord);
        Task<IBcnCredentialsRecord> GetAsync(string clientId, string assetId);
        Task<IBcnCredentialsRecord> GetByAssetAddressAsync(string assetAddress);
        Task<IEnumerable<IBcnCredentialsRecord>> GetAsync(string clientId);
        Task<string> GetClientAddress(string clientId);
    }

    public static class Ext
    {
        public static string GetAssetAddress(this IEnumerable<IBcnCredentialsRecord> creds, string assetId)
        {
            return creds.FirstOrDefault(x => x.AssetId == assetId)?.AssetAddress;
        }
    }
}
