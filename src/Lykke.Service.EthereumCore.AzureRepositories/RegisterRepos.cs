using System;
using Lykke.Service.EthereumCore.AzureRepositories.Repositories;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using AzureStorage.Tables;
using AzureStorage.Queue;
using Common.Log;
using AzureStorage.Tables.Templates.Index;
using AzureStorage.Blob;
using Lykke.SettingsReader;

namespace Lykke.Service.EthereumCore.AzureRepositories
{
    public static class RegisterReposExt
    {
        public static void RegisterAzureStorages(this IServiceCollection Services,
            IReloadingManager<BaseSettings> settings,
            IReloadingManager<SlackNotificationSettings> slackNotificationSettings)
        {
            var dataReloadingManager = settings.ConnectionString(x => x.Db.DataConnString);
            Services.AddSingleton<IPendingTransactionsRepository>(provider => new PendingTransactionsRepository(
                AzureTableStorage<PendingTransactionEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.PendingTransactions,
                    provider.GetService<ILog>()),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.PendingTransactions,
                provider.GetService<ILog>())));

            Services.AddSingleton<ITransferContractRepository>(provider => new TransferContractRepository(
                AzureTableStorage<TransferContractEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.TransferContractTable,
                provider.GetService<ILog>()),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.TransferContractTable,
                provider.GetService<ILog>())));

            Services.AddSingleton<IEventTraceRepository>(provider => new EventTraceRepository(
                AzureTableStorage<EventTraceEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.EventTraceTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<INonceRepository>(provider => new NonceRepository(
              AzureTableStorage<AddressNonceEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.NonceCacheTable,
                  provider.GetService<ILog>())));

            Services.AddSingleton<IPendingOperationRepository>(provider => new PendingOperationRepository(
                AzureTableStorage<PendingOperationEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.PendingOperationsTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IOperationToHashMatchRepository>(provider => new OperationToHashMatchRepository(
                AzureTableStorage<OperationToHashMatchEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    provider.GetService<ILog>()),
                AzureTableStorage<HashToOperationMatchEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    provider.GetService<ILog>()),
                AzureTableStorage<OperationToHashMatchHistoryEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    provider.GetService<ILog>())));


            Services.AddSingleton<IBlockSyncedRepository>(provider => new BlockSyncedRepository(
                AzureTableStorage<BlockSyncedEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.BlockSyncedTable,
                    provider.GetService<ILog>()),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.BlockSyncedTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICashinEventRepository>(provider => new CashinEventRepository(
                AzureTableStorage<CashinEventEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CashInEventTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IOwnerRepository>(provider => new OwnerRepository(
                AzureTableStorage<OwnerEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OwnerTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinEventRepository>(provider => new CoinEventRepository(
                AzureTableStorage<CoinEventEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinEventEntityTable,
                    provider.GetService<ILog>()),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinEventEntityTable,
                provider.GetService<ILog>())));

            Services.AddSingleton<IExternalTokenRepository>(provider => new ExternalTokenRepository(
                AzureTableStorage<ExternalTokenEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.ExternalTokenTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IUserPaymentHistoryRepository>(provider => new UserPaymentHistoryRepository(
                AzureTableStorage<UserPaymentHistoryEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.UserPaymentHistoryTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IUserPaymentRepository>(provider => new UserPaymentRepository());

            Services.AddSingleton<IUserTransferWalletRepository>(provider => new UserTransferWalletRepository(
               AzureTableStorage<UserTransferWalletEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.UserTransferWalletTable,
                   provider.GetService<ILog>())
                   ));

            Services.AddSingleton<IAppSettingsRepository>(provider => new AppSettingsRepository(
                AzureTableStorage<AppSettingEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.AppSettingsTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinTransactionRepository>(provider => new CoinTransactionRepository(
                AzureTableStorage<CoinTransactionEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.TransactionsTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinContractFilterRepository>(provider => new CoinContractFilterRepository(
                AzureTableStorage<CoinContractFilterEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinFiltersTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinRepository>((provider => new CoinRepository(
                AzureTableStorage<CoinEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinTable
                    , provider.GetService<ILog>())
                , AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinTableInedex
                   , provider.GetService<ILog>()))));

            Services.AddSingleton<IUserAssignmentFailRepository>(provider => new UserAssignmentFailRepository(
                AzureTableStorage<UserAssignmentFailEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.UserAssignmentFailTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IOperationResubmittRepository>(provider => new OperationResubmittRepository(
               AzureTableStorage<OperationResubmittEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationResubmittTable,
                   provider.GetService<ILog>())));

            Services.AddSingleton<IOwnerRepository>(provider => new OwnerRepository(
                AzureTableStorage<OwnerEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OwnerTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IHotWalletOperationRepository>(provider => new HotWalletOperationRepository(
                AzureTableStorage<HotWalletCashoutEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.HotWalletCashoutTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IHotWalletTransactionRepository>(provider => new HotWalletTransactionRepository(
                AzureTableStorage<HotWalletCashoutTransactionOpIdPartitionEntity>.Create(dataReloadingManager, 
                Constants.StoragePrefix + Constants.HotWalletCashoutTransactionTable,
                    provider.GetService<ILog>()),
                AzureTableStorage<HotWalletCashoutTransactionHashPartitionEntity>.Create(dataReloadingManager, 
                Constants.StoragePrefix + Constants.HotWalletCashoutTransactionTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IErc20DepositContractRepository>(provider => new Erc20DepositContractRepository(
                AzureTableStorage<Erc20DepositContractEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.Erc20DepositContractTable,
                provider.GetService<ILog>()),
                AzureTableStorage<Erc20DepositContractReversedEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.Erc20DepositContractTable,
                provider.GetService<ILog>())
                ));

            Services.AddSingleton<IGasPriceRepository>(provider => new GasPriceRepository(
                AzureTableStorage<GasPriceEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.GasPriceTable,
                    provider.GetService<ILog>())));
        }

        public static void RegisterAzureQueues(this IServiceCollection Services, 
            IReloadingManager<BaseSettings> settings, 
            IReloadingManager<SlackNotificationSettings> slackNotificationManager)
        {
            var dataReloadingManager = settings.ConnectionString(x => x.Db.DataConnString);
            var slackReloadManager = slackNotificationManager.ConnectionString(x => x.AzureQueue.ConnectionString);
            var queueName = slackNotificationManager.CurrentValue.AzureQueue.QueueName;

            Services.AddTransient<IQueueFactory, QueueFactory>();
            Services.AddTransient<Func<string, IQueueExt>>(provider =>
            {
                return (x =>
                {
                    switch (x)
                    {
                        case Constants.TransferContractUserAssignmentQueueName:
                            return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + x);
                        case Constants.EthereumContractQueue:
                            return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + x);
                        case Constants.SlackNotifierQueue:
                            return AzureQueueExt.Create(slackReloadManager, Constants.StoragePrefix + queueName);
                        case Constants.EthereumOutQueue:
                            return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + x);//remove
                        case Constants.ContractTransferQueue:
                            return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + x);
                        case Constants.TransactionMonitoringQueue:
                            return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + x);
                        case Constants.CoinTransactionQueue:
                            return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + x);
                        case Constants.UserContractManualQueue:
                            return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + x);
                        default:
                            throw new Exception("Queue is not registered");
                    }
                });
            });

        }
    }
}
