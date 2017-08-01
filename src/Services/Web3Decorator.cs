using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services
{
    public interface IWeb3
    {
        ShhApiService Shh { get; }
        EthApiContractService Eth { get; }
        IClient Client { get; }
        ITransactionManager TransactionManager { get; set; }
        PersonalApiService Personal { get; }
        NetApiService Net { get; }
    }

    public class Web3Decorator : IWeb3
    {
        private readonly Web3 _web3;

        public Web3Decorator(Web3 web3)
        {
            _web3 = web3;
        }

        public ShhApiService Shh { get { return _web3.Shh; } }
        public EthApiContractService Eth { get { return _web3.Eth; } }
        public IClient Client { get { return _web3.Client; } }
        public ITransactionManager TransactionManager { get { return _web3.TransactionManager; } set { { _web3.TransactionManager = value; } } }
        public PersonalApiService Personal { get { return _web3.Personal; } }
        public NetApiService Net { get { return _web3.Net; } }
    }
}
