using AzureRepositories.Notifiers;
using Core.Notifiers;
using Core.Settings;
using EthereumSamuraiApiCaller;
using LkeServices.Signature;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Web3;
using Services.Coins;
using Services.New;
using Services.PrivateWallet;
using Services.Signature;
using SigningServiceApiCaller;
using System;

namespace Services
{
    public static class RegisterDependency
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddTransient<IContractService, ContractService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IEthereumQueueOutService, EthereumQueueOutService>();
            services.AddTransient<IEthereumTransactionService, EthereumTransactionService>();
            services.AddTransient<IExchangeContractService, ExchangeContractService>();
            services.AddTransient<ICoinTransactionService, CoinTransactionService>();
            services.AddTransient<IErcInterfaceService, ErcInterfaceService>();
            services.AddTransient<AssetContractService>();
            services.AddTransient<TransferContractService>();
            services.AddTransient<ExternalTokenService>();
            services.AddTransient<TransferContractPoolService>();
            services.AddTransient<ITransferContractQueueService, TransferContractQueueService>();
            services.AddTransient<ITransferContractQueueServiceFactory, TransferContractQueueServiceFactory>();
            services.AddTransient<ITransferContractService, TransferContractService>();
            services.AddTransient<TransferContractUserAssignmentQueueService, TransferContractUserAssignmentQueueService>();
            services.AddTransient<ITransferContractTransactionService, TransferContractTransactionService>();
            services.AddTransient<ITransferContractUserAssignmentQueueService, TransferContractUserAssignmentQueueService>();
            services.AddTransient<ISlackNotifier, SlackNotifier>();
            services.AddTransient<ICoinEventPublisher, CoinEventPublisherService>();
            services.AddTransient<ICoinEventService, CoinEventService>();
            services.AddSingleton<IHashCalculator, HashCalculator>();
            services.AddSingleton<IPendingOperationService, PendingOperationService>();
            services.AddSingleton<ITransactionEventsService, TransactionEventsService>();
            services.AddSingleton<INonceCalculator, NonceCalculator>();
            services.AddSingleton<IPrivateWalletService, PrivateWalletService>();
            services.AddSingleton<IEthereumIndexerService, EthereumIndexerService>();
            //Uses HttpClient Inside -> singleton
            services.AddSingleton<ILykkeSigningAPI>((provider) =>
            {
                var lykkeSigningAPI = new LykkeSigningAPI(new Uri(provider.GetService<IBaseSettings>().SignatureProviderUrl
                    , UriKind.Absolute));

                return lykkeSigningAPI;
            });

            services.AddSingleton<IEthereumSamuraiApi>((provider) =>
            {
                var ethereumSamuraiApi = new EthereumSamuraiApi(new Uri(provider.GetService<IBaseSettings>().EthereumSamuraiUrl
                    , UriKind.Absolute));

                return ethereumSamuraiApi;
            });

            services.AddSingleton<Web3>((provider) =>
            {
                var baseSettings = provider.GetService<IBaseSettings>();
                var web3 = new Web3(baseSettings.EthereumUrl);

                return web3;
            });

            services.AddSingleton<IWeb3>((provider) =>
            {
                var web3 = provider.GetService<Web3>();

                return new Web3Decorator(web3);
            });


            services.AddSingleton<ITransactionManager>(provider =>
            {
                var baseSettings = provider.GetService<IBaseSettings>();
                var web3 = provider.GetService<Web3>();
                var signatureApi = provider.GetService<ILykkeSigningAPI>();
                var nonceCalculator = provider.GetService<INonceCalculator>();
                var transactionManager = new LykkeSignedTransactionManager(web3, signatureApi, baseSettings, nonceCalculator);
                web3.Client.OverridingRequestInterceptor = new SignatureInterceptor(transactionManager);

                return transactionManager;
            });
        }

        //need to fix that
        public static void ActivateRequestInterceptor(this IServiceProvider provider)
        {
            provider.GetService<ITransactionManager>();
        }
    }
}
