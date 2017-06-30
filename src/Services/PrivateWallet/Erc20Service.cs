using BusinessModels;
using BusinessModels.PrivateWallet;
using Core.Exceptions;
using Core.Settings;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Util;
using Nethereum.Web3;
using Services.Signature;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Services.PrivateWallet
{
    /*
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

    public interface IErc20Service
    {
    }

    public class Erc20Service : IErc20Service
    {
        private readonly IWeb3 _web3;
        private readonly INonceCalculator _nonceCalculator;
        private AddressUtil _addressUtil;
        private readonly IBaseSettings _settings;

        public Erc20Service(IWeb3 web3, INonceCalculator nonceCalculator, IBaseSettings settings)
        {
            _addressUtil = new AddressUtil();
            _nonceCalculator = nonceCalculator;
            _web3 = web3;
            _settings = settings;
        }

        #region transfer

        public async Task<string> GetTransferTransactionRaw(Erc20Transaction erc20Transaction)
        {
            Contract contract = GetContract(erc20Transaction.TokenAddress);
            Function transferFunction = contract.GetFunction("transfer");
            string functionDataEncoded = transferFunction.GetData();
            BigInteger nonce =  await _nonceCalculator.GetNonceAsync(erc20Transaction.FromAddress);
            var transaction = CreateTransactionInput(functionDataEncoded, erc20Transaction.TokenAddress, erc20Transaction.FromAddress,
                 erc20Transaction.GasAmount, erc20Transaction.GasPrice, nonce, 0);
            string raw = transaction.GetRLPEncoded().ToHex();

            return raw;
        }

        //put in dependency
        public async Task<string> SubmitSignedTransaction(string from, string signedTrHex)
        {
            bool isSignedRight = await CheckTransactionSign(from, signedTrHex);
            if (!isSignedRight)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "WrongSign");
            }

            var ethSendTransaction = new EthSendRawTransaction(_web3.Client);
            string transactionHex = await ethSendTransaction.SendRequestAsync(signedTrHex);

            return transactionHex;
        }

        #endregion transfer


        //put in dependency
        public async Task<bool> CheckTransactionSign(string from, string signedTrHex)
        {
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTrHex.HexToByteArray());
            string signedBy = transaction.Key.GetPublicAddress();

            return _addressUtil.ConvertToChecksumAddress(from) == _addressUtil.ConvertToChecksumAddress(signedBy);
        }

        private Contract GetContract(string erc20ContactAddress)
        {
            Contract contract = _web3.Eth.GetContract(_settings.ERC20ABI, erc20ContactAddress);

            return contract;
        }

        protected Nethereum.Signer.Transaction CreateTransactionInput(string encodedFunctionCall, string erc20ContractAddress,
            string from, BigInteger gas, BigInteger gasPrice, BigInteger nonce, BigInteger value)
        {
            return new Nethereum.Signer.Transaction(erc20ContractAddress, value, nonce, gasPrice, gas, encodedFunctionCall);
        }
    }
}
