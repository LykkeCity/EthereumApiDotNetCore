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
        EthereumContract ExternalTokenContract { get; set; }
        EthereumContract TokenAdapterContract { get; set; }
        EthereumContract EthAdapterContract { get; set; }
        Dictionary<string, EthereumContract> CoinContracts { get; set; }
        string ERC20ABI { get; set; }
        string CoinAbi { get; set; }
    }

    public class BaseSettings : IBaseSettings
    {
        public EthereumContract MainContract { get; set; }
        public EthereumContract UserContract { get; set; }
        public EthereumContract MainExchangeContract { get; set; }
        public EthereumContract TokenTransferContract { get; set; }
        public EthereumContract EthTransferContract { get; set; }
        public EthereumContract TokenAdapterContract { get; set; }
        public EthereumContract EthAdapterContract { get; set; }
        public Dictionary<string, EthereumContract> CoinContracts { get; set; } = new Dictionary<string, EthereumContract>();

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

        public EthereumContract ExternalTokenContract { get; set; }
    }

    public class EthereumContract
    {
        public string Address { get; set; }
        public string Abi { get; set; }
        public string ByteCode { get; set; }
    }

    public class DbSettings
    {
        public string DataConnString { get; set; }
        public string LogsConnString { get; set; }

        public string SharedTransactionConnString { get; set; }
        public string SharedConnString { get; set; }

        public string DictsConnString { get; set; }

        public string EthereumHandlerConnString { get; set; }
    }
}
