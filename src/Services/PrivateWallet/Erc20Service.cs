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
using Services.Transactions;
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
        Task<string> GetTransferTransactionRaw(Erc20Transaction erc20Transaction);
        Task<string> SubmitSignedTransaction(string from, string signedTrHex);
        Task ValidateInput(Erc20Transaction transaction);
    }

    public class Erc20Service : IErc20Service
    {
        private readonly IWeb3 _web3;
        private readonly INonceCalculator _nonceCalculator;
        private readonly IBaseSettings _settings;
        private readonly IRawTransactionSubmitter _rawTransactionSubmitter;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly ITransactionValidationService _transactionValidationService;
        private readonly ISignatureChecker _signatureChecker;

        public Erc20Service(IWeb3 web3, 
            INonceCalculator nonceCalculator, 
            IBaseSettings settings,
            IRawTransactionSubmitter rawTransactionSubmitter,
            IErcInterfaceService ercInterfaceService,
            ITransactionValidationService transactionValidationService,
            ISignatureChecker signatureChecker)
        {
            _rawTransactionSubmitter = rawTransactionSubmitter;
            _nonceCalculator = nonceCalculator;
            _web3 = web3;
            _settings = settings;
            _ercInterfaceService = ercInterfaceService;
            _transactionValidationService = transactionValidationService;
            _signatureChecker = signatureChecker;
        }

        #region transfer

        public async Task<string> GetTransferTransactionRaw(Erc20Transaction erc20Transaction)
        {
            Contract contract = GetContract(erc20Transaction.TokenAddress);
            Function transferFunction = contract.GetFunction("transfer");
            string functionDataEncoded = transferFunction.GetData(erc20Transaction.ToAddress, erc20Transaction.TokenAmount);
            BigInteger nonce =  await _nonceCalculator.GetNonceAsync(erc20Transaction.FromAddress);
            var transaction = CreateTransactionInput(functionDataEncoded, erc20Transaction.TokenAddress, erc20Transaction.FromAddress,
                 erc20Transaction.GasAmount, erc20Transaction.GasPrice, nonce, 0);
            string raw = transaction.GetRLPEncoded().ToHex();

            return raw;
        }

        //put in dependency
        public async Task<string> SubmitSignedTransaction(string from, string signedTrHex)
        {
            string transactionHex = await _rawTransactionSubmitter.SubmitSignedTransaction(from, signedTrHex);

            return transactionHex;
        }

        #endregion transfer

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

        public async Task ValidateInput(Erc20Transaction transaction)
        {
            await _transactionValidationService.ValidateAddressBalanceAsync(transaction.FromAddress, transaction.Value, transaction.GasAmount, transaction.GasPrice);
            await ValidateTokenAddressBalanceAsync(transaction.FromAddress, transaction.TokenAddress, transaction.TokenAmount);
        }

        public async Task ValidateInputForSignedAsync(string fromAddress, string signedTransaction)
        {
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTransaction.HexToByteArray());
            bool isSignedRight = await _signatureChecker.CheckTransactionSign(fromAddress, signedTransaction);
            string valueHex = transaction.Value.ToHex();
            string gasLimit = transaction.GasLimit.ToHex();
            string gasPrice = transaction.GasPrice.ToHex();
            string erc20InvocationData = transaction.Data.ToHexCompact();
            await _transactionValidationService.ThrowOnExistingHashAsync(transaction.Hash.ToHex());
            ThrowOnWrongSignature(isSignedRight);
            //TODO:
            //await ValidateInput(fromAddress,
            //    new HexBigInteger(transaction.Value.ToHex()),
            //    new HexBigInteger(gasLimit),
            //    new HexBigInteger(gasPrice));
        }

        public async Task ValidateTokenAddressBalanceAsync(string address, string tokenAddress, BigInteger tokenAmount)
        {
            var balance = await _ercInterfaceService.GetPendingBalanceForExternalTokenAsync(address, tokenAddress);

            if (balance < tokenAmount)
            {
                throw new ClientSideException(ExceptionType.NotEnoughFunds, "Not enough tokens");
            }
        }

        private void ThrowOnWrongSignature(bool isSignedRight)
        {
            if (!isSignedRight)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "Wrong Signature");
            }
        }
    }
}
