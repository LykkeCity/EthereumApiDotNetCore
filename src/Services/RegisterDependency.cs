using Autofac;
using Autofac.Features.AttributeFilters;
using EthereumSamuraiApiCaller;
using LkeServices.Signature;
using Lykke.Service.Assets.Client;
using Lykke.Service.EthereumCore.AzureRepositories.Notifiers;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Airlines;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Core.LykkePay;
using Lykke.Service.EthereumCore.Core.Notifiers;
using Lykke.Service.EthereumCore.Core.PassToken;
using Lykke.Service.EthereumCore.Core.PrivateWallet;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.PassTokenIntegration;
using Lykke.Service.EthereumCore.Services.Airlines;
using Lykke.Service.EthereumCore.Services.Coins;
using Lykke.Service.EthereumCore.Services.Common;
using Lykke.Service.EthereumCore.Services.Erc20;
using Lykke.Service.EthereumCore.Services.HotWallet;
using Lykke.Service.EthereumCore.Services.LykkePay;
using Lykke.Service.EthereumCore.Services.New;
using Lykke.Service.EthereumCore.Services.PassToken;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.EthereumCore.Services.Signature;
using Lykke.Service.EthereumCore.Services.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Web3;
using SigningServiceApiCaller;
using SigningServiceApiCaller.DelegatingHandlers;
using System;

namespace Lykke.Service.EthereumCore.Services
{
    public static class RegisterDependency
    {
        public static void RegisterServices(this IServiceCollection Services)
        {
            //Uses HttpClient Inside -> singleton
            Services.AddSingleton<ILykkeSigningAPI>((provider) =>
            {
                var baseSettings = provider.GetService<IBaseSettings>();
                var apiKey = baseSettings.SigningServiceApiKey;
                ApiKeyHandler apiKeyHandler = new ApiKeyHandler(apiKey);
                var lykkeSigningApi = new LykkeSigningAPI(new Uri(provider.GetService<IBaseSettings>().SignatureProviderUrl
                    , UriKind.Absolute), apiKeyHandler);

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

                return web3;
            });


            Services.AddSingleton<ITransactionManager>(provider =>
            {
                var baseSettings = provider.GetService<IBaseSettings>();
                var web3 = provider.GetService<Web3>();
                var signatureApi = provider.GetService<ILykkeSigningAPI>();
                var nonceCalculator = provider.GetService<INonceCalculator>();
                var transactionRouter = provider.GetService<ITransactionRouter>();
                var gasPriceRepository = provider.GetService<IGasPriceRepository>();
                var overrideNonceRepository = provider.GetService<IOverrideNonceRepository>();

                var transactionManager = new LykkeSignedTransactionManager(
                baseSettings, 
                nonceCalculator, 
                signatureApi, 
                transactionRouter, 
                web3, 
                gasPriceRepository,
                overrideNonceRepository);

                web3.TransactionManager = transactionManager;
                web3.Client.OverridingRequestInterceptor = new SignatureInterceptor(transactionManager);

                return transactionManager;
            });

            Services.AddSingleton<IAssetsService>((provider) =>
            {
                var settings = provider.GetService<AppSettings>();
                
                return new AssetsService(new Uri(settings.Assets.ServiceUrl));
            });

            Services.AddSingleton<IBlockPassClient>((provider) =>
            {
                var settings = provider.GetService<BlockPassClientSettings>();
                BlockPassClientFactory factory = new BlockPassClientFactory();
                var client = factory.CreateNew(settings, false);

                return client;
            });
        }

        public static void RegisterServices(this ContainerBuilder builder)
        {
            builder.RegisterType<ContractService>().As<IContractService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<PaymentService>().As<IPaymentService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<EthereumQueueOutService>().As<IEthereumQueueOutService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<EthereumTransactionService>().As<IEthereumTransactionService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<ExchangeContractService>().As<IExchangeContractService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<CoinTransactionService>().As<ICoinTransactionService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<ErcInterfaceService>().As<IErcInterfaceService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<AssetContractService>().AsSelf().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractService>().AsSelf().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<ExternalTokenService>().AsSelf().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractPoolService>().AsSelf().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractQueueService>().As<ITransferContractQueueService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractQueueServiceFactory>().As<ITransferContractQueueServiceFactory>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractService>().As<ITransferContractService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractUserAssignmentQueueService>().AsSelf().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractTransactionService>().As<ITransferContractTransactionService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransferContractUserAssignmentQueueService>().As<ITransferContractUserAssignmentQueueService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<SlackNotifier>().As<ISlackNotifier>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<CoinEventPublisherService>().As<ICoinEventPublisher>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<CoinEventService>().As<ICoinEventService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<HashCalculator>().As<IHashCalculator>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<PendingOperationService>().As<IPendingOperationService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransactionEventsService>().As<ITransactionEventsService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<NonceCalculator>().As<INonceCalculator>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<PrivateWalletService>().As<IPrivateWalletService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<EthereumIndexerService>().As<IEthereumIndexerService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<SignatureChecker>().As<ISignatureChecker>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<RawTransactionSubmitter>().As<IRawTransactionSubmitter>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20PrivateWalletService>().As<IErc20PrivateWalletService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<OwnerService>().As<IOwnerService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<OwnerBlockchainService>().As<IOwnerBlockchainService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20BalanceService>().As<IErc20BalanceService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransactionValidationService>().As<ITransactionValidationService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<SignatureService>().As<ISignatureService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<HotWalletService>().As<IHotWalletService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20DepositContractQueueServiceFactory>().As<IErc20DepositContractQueueServiceFactory>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<Erc20DepositTransactionService>().As<IErc20DepositTransactionService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<TransactionRouter>().As<ITransactionRouter>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<GasPriceService>().As<IGasPriceService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<LykkePayEventsService>().As<ILykkePayEventsService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<BlockPassService>().As<IBlockPassService>().SingleInstance().WithAttributeFiltering();
            builder.RegisterType<EstimationService>().As<IEstimationService>().SingleInstance().WithAttributeFiltering();

            builder.RegisterType<LykkePayErc20DepositContractService>()
                .Keyed<IErc20DepositContractService>(Constants.LykkePayKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<Erc20DepositContractService>()
                .Keyed<IErc20DepositContractService>(Constants.DefaultKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<Erc20ContracAssigner>()
                .Keyed<IErc20ContracAssigner>(Constants.DefaultKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<LykkePayErc20DepositContractPoolService>()
                .Keyed<IErc20DepositContractPoolService>(Constants.LykkePayKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<Erc20DepositContractPoolService>()
                .Keyed<IErc20DepositContractPoolService>(Constants.DefaultKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<AggregatedDepositContractLocatorService>()
                .Keyed<IErc20DepositContractLocatorService>(Constants.DefaultKey)
                .Keyed<IAggregatedErc20DepositContractLocatorService>(Constants.DefaultKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<LykkePayErc20DepositContractService>()
                .Keyed<IErc20DepositContractLocatorService>(Constants.LykkePayKey)
                .SingleInstance().WithAttributeFiltering();

            builder.RegisterType<AirlinesErc20DepositContractService>()
                .Keyed<IAirlinesErc20DepositContractService>(Constants.AirLinesKey)
                .Keyed<IAirlinesErc20DepositContractService>(Constants.DefaultKey)
                .Keyed<IErc20DepositContractLocatorService>(Constants.AirLinesKey)
                .SingleInstance().WithAttributeFiltering();

            #region Airlines

            builder.RegisterType<LykkePayErc20DepositContractService>()
                .Keyed<IErc20DepositContractService>(Constants.AirLinesKey)
                .SingleInstance().WithAttributeFiltering();


            builder.RegisterType<AirlinesErc20DepositContractPoolService>()
                .Keyed<IErc20DepositContractPoolService>(Constants.AirLinesKey)
                .SingleInstance().WithAttributeFiltering();

            #endregion
        }

        //TODO: need to fix that
        public static void ActivateRequestInterceptor(this IServiceProvider provider)
        {
            provider.GetService<ITransactionManager>();
        }

        public static void ActivateRequestInterceptor(this IContainer provider)
        {
            provider.Resolve<ITransactionManager>();
        }
    }
}
