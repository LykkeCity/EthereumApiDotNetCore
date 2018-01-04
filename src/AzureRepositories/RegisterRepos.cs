using System;
using Lykke.Service.EthereumCore.AzureRepositories.Log;
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

namespace Lykke.Service.EthereumCore.AzureRepositories
{
    public static class RegisterReposExt
    {
        public static void RegisterAzureLogs(this IServiceCollection Services, IBaseSettings settings, string logPrefix)
        {
            var logToTable = new LogToTable(
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Error", null),
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Warning", null),
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Info", null));

            Services.AddSingleton(logToTable);
            Services.AddTransient<LogToConsole>();

            Services.AddTransient<ILog, LogToTableAndConsole>();
        }

        public static void RegisterAzureStorages(this IServiceCollection Services, IBaseSettings settings, ISlackNotificationSettings slackNotificationSettings)
        {
            Services.AddSingleton<IPendingTransactionsRepository>(provider => new PendingTransactionsRepository(
                new AzureTableStorage<PendingTransactionEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.PendingTransactions,
                    provider.GetService<ILog>()),
                new AzureTableStorage<AzureIndex>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.PendingTransactions,
                provider.GetService<ILog>())));

            Services.AddSingleton<ITransferContractRepository>(provider => new TransferContractRepository(
                new AzureTableStorage<TransferContractEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.TransferContractTable,
                provider.GetService<ILog>()),
                new AzureTableStorage<AzureIndex>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.TransferContractTable,
                provider.GetService<ILog>())));

            Services.AddSingleton<IEventTraceRepository>(provider => new EventTraceRepository(
                new AzureTableStorage<EventTraceEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.EventTraceTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<INonceRepository>(provider => new NonceRepository(
              new AzureTableStorage<AddressNonceEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.NonceCacheTable,
                  provider.GetService<ILog>())));

            Services.AddSingleton<IPendingOperationRepository>(provider => new PendingOperationRepository(
                new AzureTableStorage<PendingOperationEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.PendingOperationsTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IOperationToHashMatchRepository>(provider => new OperationToHashMatchRepository(
                new AzureTableStorage<OperationToHashMatchEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    provider.GetService<ILog>()),
                new AzureTableStorage<HashToOperationMatchEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    provider.GetService<ILog>()),
                new AzureTableStorage<OperationToHashMatchHistoryEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    provider.GetService<ILog>())));


            Services.AddSingleton<IBlockSyncedRepository>(provider => new BlockSyncedRepository(
                new AzureTableStorage<BlockSyncedEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.BlockSyncedTable,
                    provider.GetService<ILog>()),
                new AzureTableStorage<AzureIndex>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.BlockSyncedTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICashinEventRepository>(provider => new CashinEventRepository(
                new AzureTableStorage<CashinEventEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.CashInEventTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IOwnerRepository>(provider => new OwnerRepository(
                new AzureTableStorage<OwnerEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.OwnerTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinEventRepository>(provider => new CoinEventRepository(
                new AzureTableStorage<CoinEventEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.CoinEventEntityTable,
                    provider.GetService<ILog>()),
                new AzureTableStorage<AzureIndex>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.CoinEventEntityTable,
                provider.GetService<ILog>())));

            Services.AddSingleton<IExternalTokenRepository>(provider => new ExternalTokenRepository(
                new AzureTableStorage<ExternalTokenEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.ExternalTokenTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IUserPaymentHistoryRepository>(provider => new UserPaymentHistoryRepository(
                new AzureTableStorage<UserPaymentHistoryEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.UserPaymentHistoryTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IUserPaymentRepository>(provider => new UserPaymentRepository());

            Services.AddSingleton<IUserTransferWalletRepository>(provider => new UserTransferWalletRepository(
               new AzureTableStorage<UserTransferWalletEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.UserTransferWalletTable,
                   provider.GetService<ILog>())
                   ));

            Services.AddSingleton<IAppSettingsRepository>(provider => new AppSettingsRepository(
                new AzureTableStorage<AppSettingEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.AppSettingsTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinTransactionRepository>(provider => new CoinTransactionRepository(
                new AzureTableStorage<CoinTransactionEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.TransactionsTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinContractFilterRepository>(provider => new CoinContractFilterRepository(
                new AzureTableStorage<CoinContractFilterEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.CoinFiltersTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<ICoinRepository>((provider => new CoinRepository(
                new AzureTableStorage<CoinEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.CoinTable
                    , provider.GetService<ILog>())
                , new AzureTableStorage<AzureIndex>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.CoinTableInedex
                   , provider.GetService<ILog>()))));

            Services.AddSingleton<IUserAssignmentFailRepository>(provider => new UserAssignmentFailRepository(
                new AzureTableStorage<UserAssignmentFailEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.UserAssignmentFailTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IOperationResubmittRepository>(provider => new OperationResubmittRepository(
               new AzureTableStorage<OperationResubmittEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.OperationResubmittTable,
                   provider.GetService<ILog>())));

            Services.AddSingleton<IOwnerRepository>(provider => new OwnerRepository(
                new AzureTableStorage<OwnerEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.OwnerTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IHotWalletOperationRepository>(provider => new HotWalletOperationRepository(
                new AzureTableStorage<HotWalletCashoutEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.HotWalletCashoutTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IHotWalletTransactionRepository>(provider => new HotWalletTransactionRepository(
                new AzureTableStorage<HotWalletCashoutTransactionOpIdPartitionEntity>(settings.Db.DataConnString, 
                Constants.StoragePrefix + Constants.HotWalletCashoutTransactionTable,
                    provider.GetService<ILog>()),
                new AzureTableStorage<HotWalletCashoutTransactionHashPartitionEntity>(settings.Db.DataConnString, 
                Constants.StoragePrefix + Constants.HotWalletCashoutTransactionTable,
                    provider.GetService<ILog>())));

            Services.AddSingleton<IErc20DepositContractRepository>(provider => new Erc20DepositContractRepository(
                new AzureTableStorage<Erc20DepositContractEntity>(settings.Db.DataConnString,
                Constants.StoragePrefix + Constants.Erc20DepositContractTable,
                provider.GetService<ILog>()),
                new AzureTableStorage<Erc20DepositContractReversedEntity>(settings.Db.DataConnString,
                Constants.StoragePrefix + Constants.Erc20DepositContractTable,
                provider.GetService<ILog>())
                ));

            Services.AddSingleton<IGasPriceRepository>(provider => new GasPriceRepository(
                new AzureTableStorage<GasPriceEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.GasPriceTable,
                    provider.GetService<ILog>())));
        }

        public static void RegisterAzureQueues(this IServiceCollection Services, IBaseSettings settings, ISlackNotificationSettings slackNotificationSettings)
        {
            Services.AddTransient<IQueueFactory, QueueFactory>();
            Services.AddTransient<Func<string, IQueueExt>>(provider =>
            {
                return (x =>
                {
                    switch (x)
                    {
                        case Constants.TransferContractUserAssignmentQueueName:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        case Constants.EthereumContractQueue:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        case Constants.SlackNotifierQueue:
                            return new AzureQueueExt(slackNotificationSettings.AzureQueue.ConnectionString, Constants.StoragePrefix + slackNotificationSettings.AzureQueue.QueueName);
                        case Constants.EthereumOutQueue:
                            return new AzureQueueExt(settings.Db.SharedTransactionConnString, Constants.StoragePrefix + x);//remove
                        case Constants.ContractTransferQueue:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        case Constants.TransactionMonitoringQueue:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        case Constants.CoinTransactionQueue:
                            return new AzureQueueExt(settings.Db.EthereumHandlerConnString, Constants.StoragePrefix + x);
                        case Constants.UserContractManualQueue:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        default:
                            throw new Exception("Queue is not registered");
                    }
                });
            });

        }
    }
}
