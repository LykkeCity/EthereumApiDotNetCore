using System;
using Autofac;
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
        public static void RegisterAzureStorages(this ContainerBuilder builder,
            IReloadingManager<BaseSettings> settings,
            IReloadingManager<SlackNotificationSettings> slackNotificationSettings,
            ILog log)
        {
            var dataReloadingManager = settings.ConnectionString(x => x.Db.DataConnString);
            builder.RegisterInstance<IPendingTransactionsRepository>(new PendingTransactionsRepository(
                AzureTableStorage<PendingTransactionEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.PendingTransactions,
                    log),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.PendingTransactions,
                    log)));

            builder.RegisterInstance<ITransferContractRepository>(new TransferContractRepository(
                AzureTableStorage<TransferContractEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.TransferContractTable,
                log),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.TransferContractTable,
                log)));

            builder.RegisterInstance<IEventTraceRepository>(new EventTraceRepository(
                AzureTableStorage<EventTraceEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.EventTraceTable,
                    log)));

            builder.RegisterInstance<INonceRepository>(new NonceRepository(
              AzureTableStorage<AddressNonceEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.NonceCacheTable,
                  log)));

            builder.RegisterInstance<IPendingOperationRepository>(new PendingOperationRepository(
                AzureTableStorage<PendingOperationEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.PendingOperationsTable,
                    log)));

            builder.RegisterInstance<IOperationToHashMatchRepository>(new OperationToHashMatchRepository(
                AzureTableStorage<OperationToHashMatchEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    log),
                AzureTableStorage<HashToOperationMatchEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    log),
                AzureTableStorage<OperationToHashMatchHistoryEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationToHashMatchTable,
                    log)));


            builder.RegisterInstance<IBlockSyncedRepository>(new BlockSyncedRepository(
                AzureTableStorage<BlockSyncedEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.BlockSyncedTable,
                    log),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.BlockSyncedTable,
                    log)));

            builder.RegisterInstance<ICashinEventRepository>(new CashinEventRepository(
                AzureTableStorage<CashinEventEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CashInEventTable,
                    log)));

            builder.RegisterInstance<IOwnerRepository>(new OwnerRepository(
                AzureTableStorage<OwnerEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OwnerTable,
                    log)));

            builder.RegisterInstance<ICoinEventRepository>(new CoinEventRepository(
                AzureTableStorage<CoinEventEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinEventEntityTable,
                    log),
                AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinEventEntityTable,
                log)));

            builder.RegisterInstance<IExternalTokenRepository>(new ExternalTokenRepository(
                AzureTableStorage<ExternalTokenEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.ExternalTokenTable,
                    log)));

            builder.RegisterInstance<IUserPaymentHistoryRepository>(new UserPaymentHistoryRepository(
                AzureTableStorage<UserPaymentHistoryEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.UserPaymentHistoryTable,
                    log)));

            builder.RegisterInstance<IUserPaymentRepository>(new UserPaymentRepository());

            builder.RegisterInstance<IUserTransferWalletRepository>(new UserTransferWalletRepository(
               AzureTableStorage<UserTransferWalletEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.UserTransferWalletTable,
                   log)
                   ));

            builder.RegisterInstance<IAppSettingsRepository>(new AppSettingsRepository(
                AzureTableStorage<AppSettingEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.AppSettingsTable,
                    log)));

            builder.RegisterInstance<ICoinTransactionRepository>(new CoinTransactionRepository(
                AzureTableStorage<CoinTransactionEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.TransactionsTable,
                    log)));

            builder.RegisterInstance<ICoinContractFilterRepository>(new CoinContractFilterRepository(
                AzureTableStorage<CoinContractFilterEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinFiltersTable,
                    log)));

            builder.RegisterInstance<ICoinRepository>((new CoinRepository(
                AzureTableStorage<CoinEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinTable
                    , log)
                , AzureTableStorage<AzureIndex>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.CoinTableInedex
                   , log))));

            builder.RegisterInstance<IUserAssignmentFailRepository>(new UserAssignmentFailRepository(
                AzureTableStorage<UserAssignmentFailEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.UserAssignmentFailTable,
                    log)));

            builder.RegisterInstance<IOperationResubmittRepository>(new OperationResubmittRepository(
               AzureTableStorage<OperationResubmittEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OperationResubmittTable,
                   log)));

            builder.RegisterInstance<IOwnerRepository>(new OwnerRepository(
                AzureTableStorage<OwnerEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.OwnerTable,
                    log)));

            builder.RegisterInstance<IHotWalletOperationRepository>(new HotWalletOperationRepository(
                AzureTableStorage<HotWalletCashoutEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.HotWalletCashoutTable,
                    log)));

            builder.RegisterInstance<IHotWalletTransactionRepository>(new HotWalletTransactionRepository(
                AzureTableStorage<HotWalletCashoutTransactionOpIdPartitionEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.HotWalletCashoutTransactionTable,
                    log),
                AzureTableStorage<HotWalletCashoutTransactionHashPartitionEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.HotWalletCashoutTransactionTable,
                    log)));

            builder.RegisterInstance<IErc20DepositContractRepositoryOld>(new Erc20DepositContractRepository(
                AzureTableStorage<Erc20DepositContractEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.Erc20DepositContractTable,
                log),
                AzureTableStorage<Erc20DepositContractReversedEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.Erc20DepositContractTable,
                log)
                ));

            #region Default

            builder.RegisterInstance<IErc223DepositContractRepository>(new Erc20DepositContractRepository(
                AzureTableStorage<Erc20DepositContractEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.Erc223DepositContractTable,
                log),
                AzureTableStorage<Erc20DepositContractReversedEntity>.Create(dataReloadingManager,
                Constants.StoragePrefix + Constants.Erc223DepositContractTable,
                log)
                )).Keyed<IErc223DepositContractRepository>(Constants.DefaultKey);

            #endregion

            #region LykkePay

            builder.RegisterInstance<IErc223DepositContractRepository>(new Erc20DepositContractRepository(
                AzureTableStorage<Erc20DepositContractEntity>.Create(dataReloadingManager,
                    Constants.StoragePrefix + Constants.LykkePayKey + Constants.Erc223DepositContractTable,
                    log),
                AzureTableStorage<Erc20DepositContractReversedEntity>.Create(dataReloadingManager,
                    Constants.StoragePrefix + Constants.LykkePayKey + Constants.Erc223DepositContractTable,
                    log)
            )).Keyed<IErc223DepositContractRepository>(Constants.LykkePayKey);

            #endregion

            builder.RegisterInstance<IGasPriceRepository>(new GasPriceRepository(
                AzureTableStorage<GasPriceEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.GasPriceTable,
                    log)));

            builder.RegisterInstance<IBlackListAddressesRepository>(new BlackListAddressesRepository(
                AzureTableStorage<BlackListAddressEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.BlackListAddressTable,
                    log)));

            builder.RegisterInstance<IAddressStatisticsRepository>(new AddressStatisticsRepository(
                AzureTableStorage<AddressStatisticsEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.AddressStatisticsTable,
                    log)));

            builder.RegisterInstance<IWhiteListAddressesRepository>(new WhiteListAddressesRepository(
                AzureTableStorage<WhiteListAddressesEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.WhiteListAddressesTable,
                    log)));

            Services.AddSingleton<IErc20BlackListAddressesRepository>(provider => new Erc20BlackListAddressesRepository(
                AzureTableStorage<Erc20BlackListAddressesEntity>.Create(dataReloadingManager, Constants.StoragePrefix + Constants.Erc20BlackListAddressTable,
                    provider.GetService<ILog>())));
        }

        public static void RegisterAzureQueues(this ContainerBuilder builder,
            IReloadingManager<BaseSettings> settings,
            IReloadingManager<SlackNotificationSettings> slackNotificationManager)
        {
            var dataReloadingManager = settings.ConnectionString(x => x.Db.DataConnString);
            var slackReloadManager = slackNotificationManager.ConnectionString(x => x.AzureQueue.ConnectionString);
            var queueName = slackNotificationManager.CurrentValue.AzureQueue.QueueName;
            Func<string, IQueueExt> oldQueueResolver = (queueNameArg) =>
            {
                switch (queueNameArg)
                {
                    case Constants.TransferContractUserAssignmentQueueName:
                        return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + queueNameArg);
                    case Constants.EthereumContractQueue:
                        return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + queueNameArg);
                    case Constants.SlackNotifierQueue:
                        return AzureQueueExt.Create(slackReloadManager, Constants.StoragePrefix + queueName);
                    case Constants.EthereumOutQueue:
                        return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + queueNameArg);//remove
                    case Constants.ContractTransferQueue:
                        return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + queueNameArg);
                    case Constants.TransactionMonitoringQueue:
                        return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + queueNameArg);
                    case Constants.CoinTransactionQueue:
                        return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + queueNameArg);
                    case Constants.UserContractManualQueue:
                        return AzureQueueExt.Create(dataReloadingManager, Constants.StoragePrefix + queueNameArg);
                    default:
                        throw new Exception("Queue is not registered");
                }
            };

            builder.RegisterType<QueueFactory>()
                .As<IQueueFactory>().SingleInstance();
            builder.RegisterInstance<Func<string, IQueueExt>>(oldQueueResolver);

        }
    }
}
