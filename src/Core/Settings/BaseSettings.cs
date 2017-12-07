using System;
using System.Collections.Generic;
using System.Numerics;
using Common;

namespace Core.Settings
{
    public interface IBaseSettings
    {
        string EthereumPrivateAccount { get; set; }
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

        EthereumContract MainContract { get; set; }
        EthereumContract UserContract { get; set; }
        EthereumContract MainExchangeContract { get; set; }
        EthereumContract TokenTransferContract { get; set; }
        EthereumContract EthTransferContract { get; set; }
        EthereumContract EmissiveTokenContract { get; set; }
        EthereumContract NonEmissiveTokenContract { get; set; }
        EthereumContract TokenAdapterContract { get; set; }
        EthereumContract EthAdapterContract { get; set; }
        Dictionary<string, EthereumContract> CoinContracts { get; set; }
        EthereumContract Erc20DepositContract { get; set; }
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
        public EthereumContract EmissiveTokenContract { get; set; }
        public EthereumContract NonEmissiveTokenContract { get; set; }
        public EthereumContract MainContract { get; set; }
        public EthereumContract UserContract { get; set; }
        public EthereumContract MainExchangeContract { get; set; }
        public EthereumContract TokenTransferContract { get; set; }
        public EthereumContract EthTransferContract { get; set; }
        public EthereumContract TokenAdapterContract { get; set; }
        public EthereumContract EthAdapterContract { get; set; }
        public Dictionary<string, EthereumContract> CoinContracts { get; set; } = new Dictionary<string, EthereumContract>();
        public EthereumContract Erc20DepositContract { get; set; }

        public string EthereumPrivateAccount { get; set; }

        public string EthereumMainAccount { get; set; }
        public string EthereumMainAccountPassword { get; set; }
        public string EthereumEthCoinContract { get; set; }

        /// <summary>
        /// Ethereum geth URL
        /// </summary>
        public string EthereumUrl { get; set; }
        public string SignatureProviderUrl { get; set; }

        public string EthCoin { get; set; } = "Eth";

        public DbSettings Db { get; set; }

        public int MinContractPoolLength { get; set; } = 100;
        public int MaxContractPoolLength { get; set; } = 200;
        public int ContractsPerRequest { get; set; } = 50;
        public decimal MainAccountMinBalance { get; set; } = 1.0m;

        public int Level1TransactionConfirmation { get; set; } = 2;
        public int Level2TransactionConfirmation { get; set; } = 20;
        public int Level3TransactionConfirmation { get; set; } = 100;
        public string EthereumContractBlobName { get; set; }
        public string ERC20ABI { get; set; }

        public string CoinAbi { get; set; }
        public int MaxDequeueCount { get; set; } = 1000;
        public int MaxQueueDelay { get; set; } = 5000;
        public int BroadcastMonitoringPeriodSeconds { get; set; } = 600;
        public RabbitMq RabbitMq { get; set; }
        public string MonitoringServiceUrl { get; set; }
        public int GasPricePercentage { get; set; } = 100;
        public string EthereumSamuraiUrl { get; set; }
    }

    public class EthereumContract
    {
        public string Address { get; set; }
        public string Abi { get; set; }
        public string ByteCode { get; set; }
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
        public string ServiceUrl {get;set;}
    }

    public class HotWalletSettings
    {
        public string HotwalletAddress { get; set; }
    }
}
//0ffe1e21-4dc8-44d6-bcc7-7787bf5acb06