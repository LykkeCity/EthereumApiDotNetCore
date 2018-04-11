using Lykke.Service.EthereumCore.AzureRepositories.Notifiers;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.Settings;
using EthereumSamuraiApiCaller;
using LkeServices.Signature;
using Lykke.Service.Assets.Client;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Web3;
using Lykke.Service.EthereumCore.Services.Coins;
using Lykke.Service.EthereumCore.Services.Erc20;
using Lykke.Service.EthereumCore.Services.HotWallet;
using Lykke.Service.EthereumCore.Services.New;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.EthereumCore.Services.Signature;
using Lykke.Service.EthereumCore.Services.Transactions;
using SigningServiceApiCaller;
using System;
using Autofac;
using Autofac.Features.AttributeFilters;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Services.LykkePay;

namespace Lykke.Service.EthereumCore.Services
{
    public static class RegisterDependency
    {
        public static void RegisterServices(this IServiceCollection Services)
        {
            Services.AddTransient<IContractService, ContractService>();
            Services.AddTransient<IPaymentService, PaymentService>();
            Services.AddTransient<IEthereumQueueOutService, EthereumQueueOutService>();
            Services.AddTransient<IEthereumTransactionService, EthereumTransactionService>();
            Services.AddTransient<IExchangeContractService, ExchangeContractService>();
            Services.AddTransient<ICoinTransactionService, CoinTransactionService>();
            Services.AddTransient<IErcInterfaceService, ErcInterfaceService>();
            Services.AddTransient<AssetContractService>();
            Services.AddTransient<TransferContractService>();
            Services.AddTransient<ExternalTokenService>();
            Services.AddTransient<TransferContractPoolService>();
            Services.AddTransient<ITransferContractQueueService, TransferContractQueueService>();
            Services.AddTransient<ITransferContractQueueServiceFactory, TransferContractQueueServiceFactory>();
            Services.AddTransient<ITransferContractService, TransferContractService>();
            Services.AddTransient<TransferContractUserAssignmentQueueService, TransferContractUserAssignmentQueueService>();
            Services.AddTransient<ITransferContractTransactionService, TransferContractTransactionService>();
            Services.AddTransient<ITransferContractUserAssignmentQueueService, TransferContractUserAssignmentQueueService>();
            Services.AddTransient<ISlackNotifier, SlackNotifier>();
            Services.AddTransient<ICoinEventPublisher, CoinEventPublisherService>();
            Services.AddTransient<ICoinEventService, CoinEventService>();
            Services.AddSingleton<IHashCalculator, HashCalculator>();
            Services.AddSingleton<IPendingOperationService, PendingOperationService>();
            Services.AddSingleton<ITransactionEventsService, TransactionEventsService>();
            Services.AddSingleton<INonceCalculator, NonceCalculator>();
            Services.AddSingleton<IPrivateWalletService, PrivateWalletService>();
            Services.AddSingleton<IEthereumIndexerService, EthereumIndexerService>();
            Services.AddSingleton<ISignatureChecker, SignatureChecker>();
            Services.AddSingleton<IRawTransactionSubmitter, RawTransactionSubmitter>();
            Services.AddSingleton<IErc20PrivateWalletService, Erc20PrivateWalletService>();
            Services.AddSingleton<IOwnerService, OwnerService>();
            Services.AddSingleton<IOwnerBlockchainService, OwnerBlockchainService>();
            Services.AddSingleton<IErc20BalanceService, Erc20BalanceService>();
            Services.AddSingleton<ITransactionValidationService, TransactionValidationService>();
            Services.AddSingleton<ISignatureService, SignatureService>();
            Services.AddSingleton<IHotWalletService, HotWalletService>();
            Services.AddSingleton<IErc20DepositContractQueueServiceFactory, Erc20DepositContractQueueServiceFactory>();
            Services.AddSingleton<IErc20DepositTransactionService, Erc20DepositTransactionService>();
            Services.AddSingleton<ITransactionRouter, TransactionRouter>();
            Services.AddSingleton<IGasPriceService, GasPriceService>();

            //Uses HttpClient Inside -> singleton
            Services.AddSingleton<ILykkeSigningAPI>((provider) =>
            {
                var lykkeSigningApi = new LykkeSigningAPI(new Uri(provider.GetService<IBaseSettings>().SignatureProviderUrl
                    , UriKind.Absolute));

                return lykkeSigningApi;
            });

            Services.AddSingleton<IEthereumSamuraiAPI>((provider) =>
            {
                var ethereumSamuraiApi = new EthereumSamuraiAPI(new Uri(provider.GetService<IBaseSettings>().EthereumSamuraiUrl
                    , UriKind.Absolute));

                return ethereumSamuraiApi;
            });

            Services.AddSingleton<Web3>((provider) =>
            {
                var baseSettings = provider.GetService<IBaseSettings>();
                var web3 = new Web3(baseSettings.EthereumUrl);

                return web3;
            });

            Services.AddSingleton<IWeb3>((provider) =>
            {
                var web3 = provider.GetService<Web3>();

                return new Web3Decorator(web3);
            });


            Services.AddSingleton<ITransactionManager>(provider =>
            {
                var baseSettings = provider.GetService<IBaseSettings>();
                var web3 = provider.GetService<Web3>();
                var signatureApi = provider.GetService<ILykkeSigningAPI>();
                var nonceCalculator = provider.GetService<INonceCalculator>();
                var transactionRouter = provider.GetService<ITransactionRouter>();
                var gasPriceRepository = provider.GetService<IGasPriceRepository>();

                var transactionManager = new LykkeSignedTransactionManager(baseSettings, nonceCalculator, signatureApi, transactionRouter, web3, gasPriceRepository);

                web3.TransactionManager = transactionManager;
                web3.Client.OverridingRequestInterceptor = new SignatureInterceptor(transactionManager);

                return transactionManager;
            });

            Services.AddSingleton<IAssetsService>((provider) =>
            {
                var settings = provider.GetService<AppSettings>();
                
                return new AssetsService(new Uri(settings.Assets.ServiceUrl));
            });
        }

        public static void RegisterServices(this ContainerBuilder builder)
        {
            builder.RegisterType<LykkePayErc20DepositContractService>()
                .Keyed<IErc20DepositContractService>(Constants.LykkePayKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<Erc20DepositContractService>()
                .Keyed<IErc20DepositContractService>(Constants.DefaultKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<LykkePayErc20DepositContractPoolService>()
                .Keyed<IErc20DepositContractPoolService>(Constants.LykkePayKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<Erc20DepositContractPoolService>()
                .Keyed<IErc20DepositContractPoolService>(Constants.DefaultKey)
                .SingleInstance().WithAttributeFiltering();
        }

        //TODO: need to fix that
        public static void ActivateRequestInterceptor(this IServiceProvider provider)
        {
            provider.GetService<ITransactionManager>();
        }
    }
}
