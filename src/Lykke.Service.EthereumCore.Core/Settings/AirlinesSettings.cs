using System;
using System.Collections.Generic;
using System.Numerics;
using Common;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.EthereumCore.Core.Settings
{
    public class AirlinesSettings
    {
        public EthereumContractBase Erc223DepositContract { get; set; }

        public string EthereumMainAccount { get; set; }
        public string EthereumUrl { get; set; }
        public string SignatureProviderUrl { get; set; }
        public string EthereumSamuraiUrl { get; set; }

        public DbSettings Db { get; set; }

        [Optional]
        public int MinContractPoolLength { get; set; } = 100;

        [Optional]
        public int MaxContractPoolLength { get; set; } = 200;

        [Optional]
        public int ContractsPerRequest { get; set; } = 50;

        [Optional]
        public decimal MainAccountMinBalance { get; set; } = 5.0m;

        [Optional]
        public int Level1TransactionConfirmation { get; set; } = 2;

        [Optional]
        public int Level2TransactionConfirmation { get; set; } = 20;

        [Optional]
        public int Level3TransactionConfirmation { get; set; } = 100;

        public string ERC20ABI { get; set; }

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
    }
}
//0ffe1e21-4dc8-44d6-bcc7-7787bf5acb06