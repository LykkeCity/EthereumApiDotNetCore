using System;
using AzureRepositories.Log;
using AzureRepositories.Repositories;
using Core;
using Core.Repositories;
using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using AzureStorage.Tables;
using AzureStorage.Queue;
using Common.Log;
using AzureStorage.Tables.Templates.Index;

namespace AzureRepositories
{
    public static class RegisterReposExt
    {
        public static void RegisterAzureLogs(this IServiceCollection services, IBaseSettings settings, string logPrefix)
        {
            var logToTable = new LogToTable(
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Error", null),
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Warning", null),
                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Info", null));

            services.AddSingleton(logToTable);
            services.AddTransient<LogToConsole>();

            services.AddTransient<ILog, LogToTableAndConsole>();
        }

        public static void RegisterAzureStorages(this IServiceCollection services, IBaseSettings settings)
        {
            services.AddSingleton<IMonitoringRepository>(provider => new MonitoringRepository(
                new AzureTableStorage<MonitoringEntity>(settings.Db.SharedConnString, Constants.StoragePrefix + Constants.MonitoringTable,
                    provider.GetService<ILog>())));

            services.AddSingleton<IAppSettingsRepository>(provider => new AppSettingsRepository(
                new AzureTableStorage<AppSettingEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.AppSettingsTable,
                    provider.GetService<ILog>())));

            services.AddSingleton<ICoinTransactionRepository>(provider => new CoinTransactionRepository(
                new AzureTableStorage<CoinTransactionEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.TransactionsTable,
                    provider.GetService<ILog>())));

            services.AddSingleton<ICoinContractFilterRepository>(provider => new CoinContractFilterRepository(
                new AzureTableStorage<CoinContractFilterEntity>(settings.Db.DataConnString, Constants.StoragePrefix + Constants.CoinFiltersTable,
                    provider.GetService<ILog>())));

            services.AddSingleton<ICoinRepository>((provider => new CoinRepository(
                new AzureTableStorage<CoinEntity>(settings.Db.DictsConnString, Constants.StoragePrefix + Constants.CoinTable
                    ,provider.GetService<ILog>())
                , new AzureTableStorage<AzureIndex>(settings.Db.DictsConnString, Constants.StoragePrefix + Constants.CoinTable
                   , provider.GetService<ILog>())) ));
        }

        public static void RegisterAzureQueues(this IServiceCollection services, IBaseSettings settings)
        {
            services.AddTransient<Func<string, IQueueExt>>(provider =>
            {
                return (x =>
                {
                    switch (x)
                    {
                        case Constants.EthereumContractQueue:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        case Constants.EthereumOutQueue:
                            return new AzureQueueExt(settings.Db.SharedTransactionConnString, Constants.StoragePrefix + x);
                        case Constants.EmailNotifierQueue:
                            return new AzureQueueExt(settings.Db.SharedConnString, Constants.StoragePrefix + x);
                        case Constants.ContractTransferQueue:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        case Constants.TransactionMonitoringQueue:
                            return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + x);
                        case Constants.CoinTransactionQueue:
                            return new AzureQueueExt(settings.Db.EthereumHandlerConnString, Constants.StoragePrefix + x);
                        case Constants.CoinEventQueue:
                            return new AzureQueueExt(settings.Db.SharedTransactionConnString, Constants.StoragePrefix + x);
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
