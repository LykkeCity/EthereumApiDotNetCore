using System;
using System.Collections.Generic;
using System.Numerics;
using Common;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.EthereumCore.Core.Settings
{
    public interface IBaseSettings
    {
        string EthereumMainAccount { get; set; }
        string EthereumMainAccountPassword { get; set; }

        string SignatureProviderUrl { get; set; }
        string EthereumUrl { get; set; }
        string EthCoin { get; set; }

        DbSettings Db { get; set; }

        int MinContractPoolLength { get; set; }
        int MaxContractPoolLength { get; set; }
        int ContractsPerRequest { get; set; }
        decimal MainAccountMinBalance { get; set; }

        int Level1TransactionConfirmation { get; set; }
        int Level2TransactionConfirmation { get; set; }
        int Level3TransactionConfirmation { get; set; }

        [Optional]
        EthereumContract MainContract { get; set; }
        EthereumContract MainExchangeContract { get; set; }
        EthereumContractBase TokenTransferContract { get; set; }
        EthereumContractBase EthTransferContract { get; set; }
        EthereumContractBase EmissiveTokenContract { get; set; }
        EthereumContractBase NonEmissiveTokenContract { get; set; }
        EthereumContractBase TokenAdapterContract { get; set; }
        EthereumContractBase EthAdapterContract { get; set; }
        EthereumContractBase Erc20DepositContract { get; set; }
        EthereumContractBase Erc223DepositContract { get; set; }
        string ERC20ABI { get; set; }
        string CoinAbi { get; set; }
        int MaxDequeueCount { get; set; }
        int MaxQueueDelay { get; set; }
        int BroadcastMonitoringPeriodSeconds { get; set; }
        RabbitMq RabbitMq { get; set; }
        string MonitoringServiceUrl { get; set; }
        int GasPricePercentage { get; set; }
        string EthereumSamuraiUrl { get; set; }
    }

    public class BaseSettings : IBaseSettings
    {
        public EthereumContractBase EmissiveTokenContract { get; set; }
        public EthereumContractBase NonEmissiveTokenContract { get; set; }
        [Optional]
        public EthereumContract MainContract { get; set; }
        public EthereumContract MainExchangeContract { get; set; }
        public EthereumContractBase TokenTransferContract { get; set; }
        public EthereumContractBase EthTransferContract { get; set; }
        public EthereumContractBase TokenAdapterContract { get; set; }
        public EthereumContractBase EthAdapterContract { get; set; }
        public EthereumContractBase Erc20DepositContract { get; set; }
        public EthereumContractBase Erc223DepositContract { get; set; }

        public string EthereumMainAccount { get; set; }
        public string EthereumMainAccountPassword { get; set; }

        /// <summary>
        /// Ethereum geth URL
        /// </summary>
        public string EthereumUrl { get; set; }
        public string SignatureProviderUrl { get; set; }

        [Optional]
        public string EthCoin { get; set; } = "Eth";

        public DbSettings Db { get; set; }

        [Optional]
        public int MinContractPoolLength { get; set; } = 100;

        [Optional]
        public int MaxContractPoolLength { get; set; } = 200;

        [Optional]
        public int ContractsPerRequest { get; set; } = 50;

        [Optional]
        public decimal MainAccountMinBalance { get; set; } = 1.0m;

        [Optional]
        public int Level1TransactionConfirmation { get; set; } = 2;

        [Optional]
        public int Level2TransactionConfirmation { get; set; } = 20;

        [Optional]
        public int Level3TransactionConfirmation { get; set; } = 100;

        public string ERC20ABI { get; set; }

        public string CoinAbi { get; set; }
        [Optional]
        public int MaxDequeueCount { get; set; } = 1000;

        [Optional]
        public int MaxQueueDelay { get; set; } = 5000;

        [Optional]
        public int BroadcastMonitoringPeriodSeconds { get; set; } = 600;
        public RabbitMq RabbitMq { get; set; }
        public string MonitoringServiceUrl { get; set; }

        [Optional]
        public int GasPricePercentage { get; set; } = 100;
        public string EthereumSamuraiUrl { get; set; }
    }

    public class EthereumContractBase
    {
        public string Abi { get; set; }
        public string ByteCode { get; set; }
    }

    public class EthereumContract : EthereumContractBase
    {
        public string Address { get; set; }
    }

    public class RabbitMq
    {
        public string Host { get; set; }
        public string ExternalHost { get; set; }
        public int    Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ExchangeEthereumCore { get; set; }
        public string RoutingKey { get; set; }
    }

    public class DbSettings
    {
        public string DataConnString { get; set; }
        public string LogsConnString { get; set; }

        public string SharedTransactionConnString { get; set; }
        public string DictsConnString { get; set; }

        public string EthereumHandlerConnString { get; set; }
    }

    public interface ISlackNotificationSettings
    {
        AzureQueue AzureQueue { get; set; }
    }

    public class SlackNotificationSettings : ISlackNotificationSettings
    {
        public AzureQueue AzureQueue { get; set; }
    }

    public class AzureQueue
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
    }

    public class AssetsServiceSettings
    {
        [Lykke.SettingsReader.Attributes.HttpCheck("api/isalive")]
        public string ServiceUrl {get;set;}
    }

    public class HotWalletSettings
    {
        public string HotwalletAddress { get; set; }
    }

    public class ChaosSettings
    {
        public double StateOfChaos { get; set; }
    }

    public class ApiKeys
    {
        public IEnumerable<string> Keys { get; set; }
    }

    public class LykkePay
    {
        public string LykkePayAddress { get; set; }
    }

    public class Airlines
    {
        public string AirlinesAddress { get; set; }
    }
}
//0ffe1e21-4dc8-44d6-bcc7-7787bf5acb06