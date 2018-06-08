using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CashinReportGenerator
{
    public interface IPrivateWallet
    {
        string ClientId { get; }
        string WalletAddress { get; }
        string WalletName { get; }
        string EncodedPrivateKey { get; set; }
        Lykke.Service.Assets.Client.Models.Blockchain BlockchainType { get; set; }
        bool? IsColdStorage { get; set; }
        int? Number { get; set; }
    }

    public class PrivateWallet : IPrivateWallet
    {
        public PrivateWallet() { }

        public PrivateWallet(IPrivateWallet privateWallet)
        {
            ClientId = privateWallet.ClientId;
            WalletAddress = privateWallet.WalletAddress;
            WalletName = privateWallet.WalletName;
            EncodedPrivateKey = privateWallet.EncodedPrivateKey;
            IsColdStorage = privateWallet.IsColdStorage;
            BlockchainType = privateWallet.BlockchainType;
            Number = privateWallet.Number;
        }

        public string ClientId { get; set; }
        public string WalletAddress { get; set; }
        public string EncodedPrivateKey { get; set; }
        public bool? IsColdStorage { get; set; }
        public string WalletName { get; set; }
        public Lykke.Service.Assets.Client.Models.Blockchain BlockchainType { get; set; }
        public int? Number { get; set; }
    }

    public interface IPrivateWalletsRepository
    {
        Task CreateOrUpdateWallet(IPrivateWallet wallet);
        Task RemoveWallet(string address);

        /// <summary>
        /// Returns wallet by id from stored wallets, except default.
        /// To find among all wallets use extension GetAllPrivateWallets
        /// </summary>
        /// <param name="address">wallet address</param>
        /// <returns></returns>
        Task<IPrivateWallet> GetStoredWallet(string address);

        Task<IEnumerable<IPrivateWallet>> GetAllStoredWallets(string address);

        Task<IPrivateWallet> GetStoredWalletForUser(string address, string clientId);

        /// <summary>
        /// Returns all stored wallets, except default.
        /// To get all use extension GetPrivateWallet
        /// </summary>
        /// <param name="clientId">client id</param>
        /// <returns>Private wallets enumeration</returns>
        Task<IEnumerable<IPrivateWallet>> GetStoredWallets(string clientId);
    }

    public static class PrivateWalletExt
    {
        public static async Task<IEnumerable<IPrivateWallet>> GetAllPrivateWallets(this IPrivateWalletsRepository repo, string clientId,
            IWalletCredentials walletCreds, string defaultWalletName = "default")
        {
            var wallets = new List<IPrivateWallet>()
            {
                new PrivateWallet
                {
                    ClientId = walletCreds.ClientId,
                    WalletAddress = walletCreds.Address,
                    BlockchainType = Lykke.Service.Assets.Client.Models.Blockchain.Bitcoin,
                    WalletName = defaultWalletName,
                }
            };

            var storedWallets = await repo.GetStoredWallets(clientId);
            if (storedWallets != null)
                wallets.AddRange(storedWallets);

            return wallets;
        }

        public static async Task<IPrivateWallet> GetPrivateWallet(this IPrivateWalletsRepository repo, string address, string clientId,
            IWalletCredentials walletCreds, string defaultWalletName)
        {
            var wallet = await repo.GetStoredWalletForUser(address, clientId);

            if (wallet == null && walletCreds.Address == address)
            {
                wallet = new PrivateWallet
                {
                    ClientId = walletCreds.ClientId,
                    WalletAddress = walletCreds.Address,
                    WalletName = defaultWalletName,
                    BlockchainType = Lykke.Service.Assets.Client.Models.Blockchain.Bitcoin,
                    Number = null
                };
            }

            return wallet;
        }
    }

    public interface IWalletCredentials
    {
        string ClientId { get; }
        string Address { get; }
        string PublicKey { get; }
        string PrivateKey { get; }
        string MultiSig { get; }
        string ColoredMultiSig { get; }
        bool PreventTxDetection { get; }
        string EncodedPrivateKey { get; }

        /// <summary>
        /// Conversion wallet is used for accepting BTC deposit and transfering needed LKK amount
        /// </summary>
        string BtcConvertionWalletPrivateKey { get; set; }
        string BtcConvertionWalletAddress { get; set; }

        /// <summary>
        /// Eth contract for user
        /// </summary>
        //ToDo: rename field to EthContract and change existing records
        string EthConversionWalletAddress { get; set; }
        string EthAddress { get; set; }
        string EthPublicKey { get; set; }

        string SolarCoinWalletAddress { get; set; }

        string ChronoBankContract { get; set; }

        string QuantaContract { get; set; }
    }

    public class WalletCredentials : IWalletCredentials
    {
        public string ClientId { get; set; }
        public string Address { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string MultiSig { get; set; }
        public string ColoredMultiSig { get; set; }
        public bool PreventTxDetection { get; set; }
        public string EncodedPrivateKey { get; set; }

        /// <summary>
        /// Conversion wallet is used for accepting BTC deposit and transfering needed LKK amount
        /// </summary>
        public string BtcConvertionWalletPrivateKey { get; set; }
        public string BtcConvertionWalletAddress { get; set; }

        //EthContract in fact. ToDo: rename
        public string EthConversionWalletAddress { get; set; }
        public string EthAddress { get; set; }
        public string EthPublicKey { get; set; }

        public string SolarCoinWalletAddress { get; set; }
        public string ChronoBankContract { get; set; }
        public string QuantaContract { get; set; }

        public static WalletCredentials Create(string clientId, string address, string privateKey,
            string multisig, string coloredMultiSig, string btcConvertionWalletPrivateKey,
            string btcConvertionWalletAddress, bool preventTxDetection = false,
            string encodedPk = "", string pubKey = "")
        {
            return new WalletCredentials
            {
                ClientId = clientId,
                Address = address,
                PublicKey = pubKey,
                PrivateKey = privateKey,
                MultiSig = multisig,
                ColoredMultiSig = coloredMultiSig,
                PreventTxDetection = preventTxDetection,
                EncodedPrivateKey = encodedPk,
                BtcConvertionWalletPrivateKey = btcConvertionWalletPrivateKey,
                BtcConvertionWalletAddress = btcConvertionWalletAddress
            };
        }

        public static WalletCredentials Create(IWalletCredentials src)
        {
            return new WalletCredentials
            {
                ClientId = src.ClientId,
                Address = src.Address,
                PrivateKey = src.PrivateKey,
                MultiSig = src.MultiSig,
                ColoredMultiSig = src.ColoredMultiSig,
                PreventTxDetection = src.PreventTxDetection,
                EncodedPrivateKey = src.EncodedPrivateKey,
                PublicKey = src.PublicKey,
                BtcConvertionWalletPrivateKey = src.BtcConvertionWalletPrivateKey,
                BtcConvertionWalletAddress = src.BtcConvertionWalletAddress,
                EthConversionWalletAddress = src.EthConversionWalletAddress,
                EthAddress = src.EthAddress,
                EthPublicKey = src.EthPublicKey
            };
        }
    }

    public interface IWalletCredentialsRepository
    {
        /// <summary>
        /// Сохранить сгенеренные данные по бит коину
        /// </summary>
        /// <param name="walletCredentials"></param>
        /// <returns></returns>
        Task SaveAsync(IWalletCredentials walletCredentials);

        Task MergeAsync(IWalletCredentials walletCredentials);

        Task<IWalletCredentials> GetAsync(string clientId);

        Task<IWalletCredentials> GetByEthConversionWalletAsync(string ethWallet);

        Task<IWalletCredentials> GetBySolarCoinWalletAsync(string address);

        Task<IWalletCredentials> GetByChronoBankContractAsync(string contract);

        Task<IWalletCredentials> GetByQuantaContractAsync(string contract);

        Task<string> GetClientIdByMultisig(string multisig);

        Task SetPreventTxDetection(string clientId, bool value);

        Task SetEncodedPrivateKey(string clientId, string encodedPrivateKey);

        Task SetEthConversionWallet(string clientId, string contract);

        Task SetEthFieldsWallet(string clientId, string contract, string address, string pubKey);

        Task SetSolarCoinWallet(string clientId, string address);

        Task SetChronoBankContract(string clientId, string contract);

        Task SetQuantaContract(string clientId, string contract);

        Task<IWalletCredentials> ScanAndFind(Func<IWalletCredentials, bool> item);

        Task ScanAllAsync(Func<IEnumerable<IWalletCredentials>, Task> chunk);
    }

    public interface IWalletCredentialsHistoryRepository
    {
        Task InsertHistoryRecord(IWalletCredentials oldWalletCredentials);
        Task<IEnumerable<string>> GetPrevMultisigsForUser(string clientId);
    }

    public class PrivateWalletEntity : TableEntity, IPrivateWallet
    {
        public string ClientId { get; set; }
        public string WalletAddress { get; set; }
        public string WalletName { get; set; }
        public string EncodedPrivateKey { get; set; }
        public bool? IsColdStorage { get; set; }
        public int? Number { get; set; }
        public string BlockchainTypeFlat { get; set; }
        public Lykke.Service.Assets.Client.Models.Blockchain BlockchainType
        {
            get
            {
                Enum.TryParse(BlockchainTypeFlat, true, out Lykke.Service.Assets.Client.Models.Blockchain result);

                return result;
            }
            set
            {
                BlockchainTypeFlat = value.ToString();
            }
        }

        public static class ByClient
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            public static string GenerateRowKey(string address)
            {
                return address;
            }

            public static PrivateWalletEntity Create(IPrivateWallet privateWallet)
            {
                return new PrivateWalletEntity
                {
                    PartitionKey = GeneratePartitionKey(privateWallet.ClientId),
                    RowKey = GenerateRowKey(privateWallet.WalletAddress),
                    ClientId = privateWallet.ClientId,
                    WalletName = privateWallet.WalletName,
                    WalletAddress = privateWallet.WalletAddress,
                    EncodedPrivateKey = privateWallet.EncodedPrivateKey,
                    IsColdStorage = privateWallet.IsColdStorage,
                    BlockchainType = privateWallet.BlockchainType,
                    Number = privateWallet.Number,
                };
            }
        }

        public static class Record
        {
            public static string GeneratePartitionKey()
            {
                return "PrivateWallet";
            }

            public static string GenerateRowKey(string address)
            {
                return address;
            }

            public static PrivateWalletEntity Create(IPrivateWallet privateWallet)
            {
                return new PrivateWalletEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = GenerateRowKey(privateWallet.WalletAddress),
                    ClientId = privateWallet.ClientId,
                    WalletName = privateWallet.WalletName,
                    WalletAddress = privateWallet.WalletAddress,
                    EncodedPrivateKey = privateWallet.EncodedPrivateKey,
                    IsColdStorage = privateWallet.IsColdStorage,
                    BlockchainType = privateWallet.BlockchainType,
                    Number = privateWallet.Number
                };
            }
        }
    }

    public class PrivateWalletsRepository : IPrivateWalletsRepository
    {
        private readonly INoSQLTableStorage<PrivateWalletEntity> _tableStorage;

        public PrivateWalletsRepository(INoSQLTableStorage<PrivateWalletEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task CreateOrUpdateWallet(IPrivateWallet wallet)
        {
            var entityByClient = PrivateWalletEntity.ByClient.Create(wallet);
            var entity = PrivateWalletEntity.Record.Create(wallet);
            await _tableStorage.InsertOrMergeAsync(entity);
            await _tableStorage.InsertOrMergeAsync(entityByClient);
        }

        public async Task RemoveWallet(string address)
        {
            var rowKey = PrivateWalletEntity.Record.GenerateRowKey(address);
            var partition = PrivateWalletEntity.Record.GeneratePartitionKey();
            var entity = await _tableStorage.GetDataAsync(partition, rowKey);

            await _tableStorage.DeleteAsync(partition, rowKey);
            await _tableStorage.DeleteAsync(PrivateWalletEntity.ByClient.GeneratePartitionKey(entity.ClientId),
                PrivateWalletEntity.ByClient.GenerateRowKey(address));
        }

        public async Task<IPrivateWallet> GetStoredWallet(string address)
        {
            var rowKey = PrivateWalletEntity.Record.GenerateRowKey(address);
            var partition = PrivateWalletEntity.Record.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(partition, rowKey);
        }

        public async Task<IEnumerable<IPrivateWallet>> GetAllStoredWallets(string address)
        {
            var rowKey = PrivateWalletEntity.Record.GenerateRowKey(address);
            return await _tableStorage.GetDataAsync(x => x.RowKey == rowKey);
        }

        public async Task<IPrivateWallet> GetStoredWalletForUser(string address, string clientId)
        {
            var rowKey = PrivateWalletEntity.ByClient.GenerateRowKey(address);
            var partition = PrivateWalletEntity.ByClient.GeneratePartitionKey(clientId);
            return await _tableStorage.GetDataAsync(partition, rowKey);
        }

        public async Task<IEnumerable<IPrivateWallet>> GetStoredWallets(string clientId)
        {
            var partition = PrivateWalletEntity.ByClient.GeneratePartitionKey(clientId);
            return await _tableStorage.GetDataAsync(partition);
        }

        public async Task ProcessAllAsync(Func<IPrivateWallet, Task> processAction)
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
}
