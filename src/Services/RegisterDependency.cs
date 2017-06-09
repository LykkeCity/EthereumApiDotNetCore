using AzureRepositories.Notifiers;
using Core.Notifiers;
using Core.Settings;
using LkeServices.Signature;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Web3;
using Services.Coins;
using Services.New;
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
            //Uses HttpClient Inside -> singleton
            services.AddSingleton<ILykkeSigningAPI>((provider) =>
            {
                var lykkeSigningAPI = new LykkeSigningAPI(new Uri(provider.GetService<IBaseSettings>().SignatureProviderUrl, UriKind.Absolute));

                return lykkeSigningAPI;
            });

            services.AddSingleton<Web3>((provider) =>
            {
                var web3 = new Web3(provider.GetService<IBaseSettings>().EthereumUrl);
                web3.Client.OverridingRequestInterceptor = new SignatureInterceptor(provider.GetService<ILykkeSigningAPI>(), web3);

                return web3;
            });

        }
    }
}
