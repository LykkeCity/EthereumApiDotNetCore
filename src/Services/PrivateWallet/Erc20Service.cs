using Lykke.Service.EthereumCore.BusinessModels.PrivateWallet;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services.Signature;
using Lykke.Service.EthereumCore.Services.Transactions;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;

namespace Lykke.Service.EthereumCore.Services.PrivateWallet
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

    public interface IErc20PrivateWalletService
    {
        Task<string> GetTransferTransactionRaw(Erc20Transaction erc20Transaction, bool useTxPool = false);
        Task<string> SubmitSignedTransaction(string from, string signedTrHex);
        Task ValidateInput(Erc20Transaction transaction);
        Task ValidateInputForSignedAsync(string fromAddress, string signedTransaction);
        string GetTransferFunctionCallEncoded(string tokenAddress, string receiverAddress, BigInteger amount);
    }

    public class Erc20PrivateWalletService : IErc20PrivateWalletService
    {
        private readonly IWeb3 _web3;
        private readonly INonceCalculator _nonceCalculator;
        private readonly IBaseSettings _settings;
        private readonly IRawTransactionSubmitter _rawTransactionSubmitter;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly ITransactionValidationService _transactionValidationService;
        private readonly IBaseSettings _baseSettings;

        public Erc20PrivateWalletService(IWeb3 web3, 
            INonceCalculator nonceCalculator, 
            IBaseSettings settings,
            IRawTransactionSubmitter rawTransactionSubmitter,
            IErcInterfaceService ercInterfaceService,
            ITransactionValidationService transactionValidationService,
            IBaseSettings baseSettings)
        {
            _rawTransactionSubmitter      = rawTransactionSubmitter;
            _nonceCalculator              = nonceCalculator;
            _web3                         = web3;
            _settings                     = settings;
            _ercInterfaceService          = ercInterfaceService;
            _transactionValidationService = transactionValidationService;
            _baseSettings = baseSettings;
        }

        #region transfer

        public string GetTransferFunctionCallEncoded(string tokenAddress, string receiverAddress, BigInteger amount)
        {
            Contract contract = GetContract(tokenAddress);
            Function transferFunction = contract.GetFunction("transfer");
            string functionDataEncoded = transferFunction.GetData(receiverAddress, amount);
            return functionDataEncoded;
        }

        public async Task<string> GetTransferTransactionRaw(Erc20Transaction erc20Transaction, bool useTxPool = false)
        {
            var functionDataEncoded = GetTransferFunctionCallEncoded(
                erc20Transaction.TokenAddress, 
                erc20Transaction.ToAddress, 
                erc20Transaction.TokenAmount);
            BigInteger nonce           =  await _nonceCalculator.GetNonceAsync(erc20Transaction.FromAddress, useTxPool);
            var transaction            = CreateTransactionInput(functionDataEncoded, erc20Transaction.TokenAddress, erc20Transaction.FromAddress,
                 erc20Transaction.GasAmount, erc20Transaction.GasPrice, nonce, 0);
            string raw                 = transaction.GetRLPEncoded().ToHex();

            return raw;
        }

        //put in dependency
        public async Task<string> SubmitSignedTransaction(string from, string signedTrHex)
        {
            await ValidateInputForSignedAsync(from, signedTrHex);
            string transactionHex = await _rawTransactionSubmitter.SubmitSignedTransaction(from, signedTrHex);

            return transactionHex;
        }

        #endregion transfer

        private Contract GetContract(string erc20ContactAddress)
        {
            Contract contract = _web3.Eth.GetContract(_settings.ERC20ABI, erc20ContactAddress);

            return contract;
        }

        protected Nethereum.Signer.TransactionChainId CreateTransactionInput(string encodedFunctionCall, string erc20ContractAddress,
            string from, BigInteger gas, BigInteger gasPrice, BigInteger nonce, BigInteger value)
        {
            return new Nethereum.Signer.TransactionChainId(erc20ContractAddress, value, nonce, gasPrice, gas, encodedFunctionCall,
                _baseSettings.ChainId);
        }

        public async Task ValidateInput(Erc20Transaction transaction)
        {
            await _transactionValidationService.ValidateAddressBalanceAsync(transaction.FromAddress, transaction.Value, transaction.GasAmount, transaction.GasPrice);
            await ValidateTokenAddressBalanceAsync(transaction.FromAddress, transaction.TokenAddress, transaction.TokenAmount);
        }

        /// Example for debugging purpose: signedTransaction - 0xa9059cbb000000000000000000000000aa4981d084120aef4bbaeecb9abdbc7d180c7edb000000000000000000000000000000000000000000000000000000000000000a
        public async Task ValidateInputForSignedAsync(string fromAddress, string signedTransaction)
        {
            await _transactionValidationService.ValidateInputForSignedAsync(fromAddress, signedTransaction);
            var transaction = new Nethereum.Signer.TransactionChainId(signedTransaction.HexToByteArray());
            string erc20Address                      = transaction.ReceiveAddress.ToHex().EnsureHexPrefix();
            string erc20InvocationData               = transaction.Data.ToHex().EnsureHexPrefix();
            
            if (! await _transactionValidationService.IsTransactionErc20Transfer(signedTransaction))
            {
                throw new ClientSideException(ExceptionType.WrongParams, "Transaction is not a erc20 transfer");
            }

            string parametrsString    = erc20InvocationData.Replace(Constants.Erc20TransferSignature, "");
            var amount                = parametrsString.Substring(64, 64);
            HexBigInteger tokenAmount = new HexBigInteger(amount);

            await ValidateTokenAddressBalanceAsync(fromAddress, erc20Address, tokenAmount);
        }

        public async Task ValidateTokenAddressBalanceAsync(string address, string tokenAddress, BigInteger tokenAmount)
        {
            var balance = await _ercInterfaceService.GetPendingBalanceForExternalTokenAsync(address, tokenAddress);

            if (balance < tokenAmount)
            {
                throw new ClientSideException(ExceptionType.NotEnoughFunds, "Not enough tokens");
            }
        }
    }
}
