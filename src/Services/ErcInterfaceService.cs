using Core.Settings;
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

    //TODO: compile ERC20 contract
    public class ErcInterfaceService
    {
        private readonly BaseSettings _settings;

        public ErcInterfaceService(BaseSettings settings)
        {
            _settings = settings;
        }

        public async Task<BigInteger> GetBalanceForExternalToken(string transferContractAddress,string externalTokenAddress)
        {
            Web3 web3 = new Web3(_settings.EthereumUrl);
            Contract contract = web3.Eth.GetContract(_settings.ERC20ABI, externalTokenAddress);
            Function function = contract.GetFunction("balanceOf");

            string transactionResult = 
                await function.SendTransactionAsync(_settings.EthereumMainAccount, transferContractAddress);

            HexBigInteger bigInt = new HexBigInteger(transactionResult);

            return bigInt.Value;
         }
    }
}
