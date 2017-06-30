using Core.Settings;
using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Services
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
        Task<BigInteger> GetBalanceForExternalTokenAsync(string transferContractAddress, string externalTokenAddress);
        Task<string> Transfer(string externalTokenAddress, string fromAddress, string toAddress, BigInteger amount);
    }

    public class ErcInterfaceService : IErcInterfaceService
    {
        private readonly IBaseSettings _settings;
        private readonly IWeb3 _web3;

        public ErcInterfaceService(IBaseSettings settings, IWeb3 web3)
        {
            _web3 = web3;
            _settings = settings;
        }

        public async Task<BigInteger> GetBalanceForExternalTokenAsync(string transferContractAddress, string externalTokenAddress)
        {
            Contract contract = _web3.Eth.GetContract(_settings.ERC20ABI, externalTokenAddress);
            Function function = contract.GetFunction("balanceOf");

            BigInteger result = await function.CallAsync<BigInteger>(transferContractAddress);

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
            string toAddress, BigInteger amount)
        {
            Contract contract = _web3.Eth.GetContract(_settings.ERC20ABI, externalTokenAddress);
            Function function = contract.GetFunction("transfer");
        
            //bool success = await function.CallAsync<bool>(toAddress, amount);

            string trHash = await function.SendTransactionAsync(fromAddress, toAddress, amount);

            return trHash;
        }
    }
}
