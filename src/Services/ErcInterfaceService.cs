using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Services;
using Nethereum.ABI.Encoders;

namespace Lykke.Service.EthereumCore.Services
{
    /*
    pragma solidity ^0.4.4;

    contract ERC20Interface {
    event Transfer(address indexed from, address indexed to, uint256 value);
    event Approval(address indexed from, address indexed spender, uint256 value);

    function totalSupply() constant returns (uint256 supply);
    function balanceOf(address _owner) constant returns (uint256 balance);
    function transfer(address _to, uint256 _value) returns (bool success);
    function transferFrom(address _from, address _to, uint256 _value) returns (bool success);
    function approve(address _spender, uint256 _value) returns (bool success);
    function allowance(address _owner, address _spender) constant returns (uint256 remaining);
    }
     */

    public interface IErcInterfaceService
    {
        Task<BigInteger> GetPendingBalanceForExternalTokenAsync(string address, string externalTokenAddress);
        Task<BigInteger> GetBalanceForExternalTokenAsync(string address, string externalTokenAddress);
        Task<string> Transfer(string externalTokenAddress, string fromAddress, string toAddress, BigInteger amount, byte[] bytes = null);
        Task<bool> CheckTokenFallback(string toAddress);
    }

    public class ErcInterfaceService : IErcInterfaceService
    {
        public const string Erc223ReceiverAbi =
            "[{\"constant\":true,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"},{\"name\":\"_data\",\"type\":\"bytes\"}],\"name\":\"tokenFallback\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"pure\",\"type\":\"function\"}]";

        //no transfer function in this abi for (string toAddress, BigInteger amount)
        public const string Erc223TokenAbi =
            "[{\"constant\":false,\"inputs\":[{\"name\":\"_spender\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"name\":\"supply\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_who\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"},{\"name\":\"_data\",\"type\":\"bytes\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"},{\"name\":\"_spender\",\"type\":\"address\"}],\"name\":\"allowance\",\"outputs\":[{\"name\":\"remaining\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"_from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"_to\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"_from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"_to\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"_value\",\"type\":\"uint256\"},{\"indexed\":false,\"name\":\"_data\",\"type\":\"bytes\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"_from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"_spender\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"}]";
        private readonly IBaseSettings _settings;
        private readonly IWeb3 _web3;

        public ErcInterfaceService(IBaseSettings settings, IWeb3 web3)
        {
            _web3 = web3;
            _settings = settings;
        }

        public async Task<BigInteger> GetPendingBalanceForExternalTokenAsync(string address, string externalTokenAddress)
        {
            Function function = GetBalanceOfFunction(externalTokenAddress);
            BigInteger result = await function.CallAsync<BigInteger>(BlockParameter.CreatePending(), address);

            return result;
        }

        public async Task<BigInteger> GetBalanceForExternalTokenAsync(string address, string externalTokenAddress)
        {
            Function function = GetBalanceOfFunction(externalTokenAddress);
            BigInteger result = await function.CallAsync<BigInteger>(address);

            return result;
        }

        //It's a TRAP! (allowance)
        public async Task<bool> TransferFrom(string externalTokenAddress, string fromAddress,
            string toAddress, BigInteger amount)
        {
            Contract contract = _web3.Eth.GetContract(_settings.ERC20ABI, externalTokenAddress);
            Function function = contract.GetFunction("transferFrom");

            bool success = await function.CallAsync<bool>(fromAddress, toAddress, amount);

            string trHash = await function.SendTransactionAsync(_settings.EthereumMainAccount,
                fromAddress, toAddress, new HexBigInteger(amount));

            return success;
        }


        //Use function below to transfer from main
        public async Task<string> Transfer(string externalTokenAddress, string fromAddress,
            string toAddress, BigInteger amount, byte[] bytes = null)
        {
            if (bytes == null)
            {
                Contract contract = _web3.Eth.GetContract(_settings.ERC20ABI, externalTokenAddress);
                Function function = contract.GetFunction("transfer");
                string trHash = await function.SendTransactionAsync(fromAddress, toAddress, amount);

                return trHash;
            }
            else
            {
                Contract contract = _web3.Eth.GetContract(Erc223TokenAbi, externalTokenAddress);
                Function function = contract.GetFunction("transfer");
                string trHash = await function.SendTransactionAsync(fromAddress, toAddress, amount, bytes);

                return trHash;
            }
        }

        /*
       "functionHashes": {
           "tokenFallback(address,uint256,bytes)": "c0ee0b8a"
        },
        */
        public async Task<bool> CheckTokenFallback(string toAddress)
        {
            var addressCode = await _web3.Eth.GetCode.SendRequestAsync(toAddress);
            if (addressCode == "0x")
            {
                //Account controlled by external private key - passes check
                return true;
            }
            else
            {
                var callResult =
                    await _web3.Eth.GetContract(Erc223ReceiverAbi, toAddress).GetFunction("tokenFallback").CallAsync<string>(toAddress, 1, new byte[]{});
            }

            return false;
        }

        private Function GetBalanceOfFunction(string externalTokenAddress)
        {
            Contract contract = _web3.Eth.GetContract(_settings.ERC20ABI, externalTokenAddress);
            Function function = contract.GetFunction("balanceOf");
            return function;
        }
    }
}
