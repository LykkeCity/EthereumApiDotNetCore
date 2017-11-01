using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.EthereumCore.Contracts.Events
{
    public class CoinAdapterCreationEvent
    {
        public string Blockchain              { get; private set; }
        public string Id                      { get; private set; }
        public string Name                    { get; private set; }
        public string AdapterAddress          { get; private set; }
        public string ExternalTokenAddress    { get; private set; }
        public int Multiplier                 { get; private set; }
        public bool BlockchainDepositEnabled  { get; private set; }
        public bool ContainsEth               { get; private set; }
        public string DeployedTransactionHash { get; private set; }

        public CoinAdapterCreationEvent(string coinAdapterAddress,
            string blockchain,
            bool depositEnabled, 
            bool containsEth,
            string deployedTrHash,
            string externalTokenAddress, 
            string coinId, 
            int multiplier, 
            string name)
        {
            AdapterAddress           = coinAdapterAddress;
            Blockchain               = blockchain;
            BlockchainDepositEnabled = depositEnabled;
            ContainsEth              = containsEth;
            DeployedTransactionHash  = deployedTrHash;
            ExternalTokenAddress     = externalTokenAddress;
            Id                       = coinId;
            Multiplier               = multiplier;
            Name                     = name;
        }
    }
}
