using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.RPC.Eth.TransactionManagers;
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
        TransactionSigner OfflineTransactionSigner { get; }
        UnitConversion Convert { get; }
        ITransactionManager TransactionManager { get; set; }
        PersonalApiService Personal { get; }
        NetApiService Net { get; }

        string GetAddressFromPrivateKey(string privateKey);
        bool IsChecksumAddress(string address);
        string Sha3(string value);
        string ToChecksumAddress(string address);
        string ToValid20ByteAddress(string address);
    }

    public class Web3Decorator : IWeb3
    {
        private readonly Web3 _web3;

        public Web3Decorator(Web3 web3)
        {
            _web3 = web3;
        }

        public ShhApiService Shh => _web3.Shh;

        public EthApiContractService Eth => _web3.Eth;

        public IClient Client => _web3.Client;

        public TransactionSigner OfflineTransactionSigner => _web3.OfflineTransactionSigner;

        public UnitConversion Convert => _web3.Convert;

        public ITransactionManager TransactionManager { get => _web3.TransactionManager; set => _web3.TransactionManager = value; }

        public PersonalApiService Personal => _web3.Personal;

        public NetApiService Net => _web3.Net;

        public string GetAddressFromPrivateKey(string privateKey)
        {
            return _web3.GetAddressFromPrivateKey(privateKey);
        }

        public bool IsChecksumAddress(string address)
        {
            return _web3.IsChecksumAddress(address);
        }

        public string Sha3(string value)
        {
            return _web3.Sha3(value);
        }

        public string ToChecksumAddress(string address)
        {
            return _web3.ToChecksumAddress(address);
        }

        public string ToValid20ByteAddress(string address)
        {
            return _web3.ToValid20ByteAddress(address);
        }
    }
}
