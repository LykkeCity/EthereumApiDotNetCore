using Common.Log;
using Core;
using Core.Exceptions;
using Core.Notifiers;
using Core.Repositories;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Services
{
    public class ExternalTokenService
    {
        private readonly IContractService _contractService;
        private readonly ILog _logger;
        private readonly IBaseSettings _settings;
        private readonly IExternalTokenRepository _tokenRepository;
        private readonly IErcInterfaceService _ercInterfaceService;

        public ExternalTokenService(IExternalTokenRepository tokenRepository,
            IContractService contractService,
            ILog logger,
            IBaseSettings settings,
            IErcInterfaceService ercInterfaceService)
        {
            _contractService = contractService;
            _tokenRepository = tokenRepository;
            _logger = logger;
            _settings = settings;
            _ercInterfaceService = ercInterfaceService;
        }

        //Lykke is the owner of that token
        public async Task<IEnumerable<IExternalToken>> GetAllTokensAsync()
        {
            IEnumerable<IExternalToken> result = await _tokenRepository.GetAllAsync();

            return result;
        }

        //Lykke is the owner of that token
        /*
            string tokenName,
            uint8 divisibility,
            string tokenSymbol, 
            string version
        */
        public async Task<IExternalToken> CreateExternalTokenAsync(string tokenName, byte divisibility, string tokenSymbol, string version, bool allowEmission, BigInteger initialSupply)
        {
            string id = Guid.NewGuid().ToString();
            List<object> @params = new List<object>() { _settings.EthereumMainAccount, tokenName, divisibility, tokenSymbol, version };
            string abi = allowEmission ? _settings.EmissiveTokenContract.Abi : _settings.NonEmissiveTokenContract.Abi;
            string byteCode = allowEmission ? _settings.EmissiveTokenContract.ByteCode : _settings.NonEmissiveTokenContract.ByteCode;
            if (!allowEmission)
            {
                @params.Add(initialSupply);
            }

            string contractAddress = await _contractService.CreateContract(abi,
                byteCode, @params.ToArray());

            IExternalToken externalToken = new ExternalToken()
            {
                Id = id,
                Name = tokenName,
                ContractAddress = contractAddress,
                Divisibility = divisibility,
                InitialSupply = initialSupply.ToString(),
                TokenSymbol = tokenSymbol,
                Version = version
            };

            await _tokenRepository.SaveAsync(externalToken);

            return externalToken;
        }

        public Task<IExternalToken> CreateExternalTokenAsync(string tokenName, object divisibility, string tokenSymbol, string version, bool allowEmission, BigInteger amount)
        {
            throw new NotImplementedException();
        }

        public async Task<IExternalToken> GetByAddressAsync(string externalTokenAddress)
        {
            IExternalToken result = await _tokenRepository.GetAsync(externalTokenAddress);

            return result;
        }

        public async Task<string> IssueTokensAsync(string externalTokenAddress, string toAddress, BigInteger amount)
        {
            var externalToken = await _tokenRepository.GetAsync(externalTokenAddress);
            if (externalToken == null)
            {
                throw new ClientSideException(ExceptionType.WrongParams, $"External Token With address {externalTokenAddress} does not exist!");
            }
            //0x2dcc0430f6b9a40fd54f7d99f8a73b7d2d84b1fa
            var initialSupply = BigInteger.Parse(externalToken.InitialSupply);
            if (initialSupply != 0)
            {
                BigInteger balance = await _ercInterfaceService.GetBalanceForExternalTokenAsync(_settings.EthereumMainAccount, externalTokenAddress);
                if (balance < amount)
                {
                    string message = $"Can't issue more tokens. Current balance is {balance}, issue amount is {amount}.";
                    await _logger.WriteInfoAsync("ExternalTokenService", "IssueTokens", "", message, DateTime.UtcNow);
                    throw new ClientSideException(ExceptionType.WrongParams, message);
                }
            }

            string trHash = await _ercInterfaceService.Transfer(externalTokenAddress, _settings.EthereumMainAccount, toAddress, amount);

            await _logger.WriteInfoAsync("ExternalTokenService", "IssueTokens", "", $"Issued to address {toAddress} {amount.ToString()} tokens from {externalTokenAddress}", DateTime.UtcNow);

            return trHash;
        }

        public async Task<BigInteger> GetBalance(string externalTokenAddress, string ownerAddress)
        {
            var externalToken = await _tokenRepository.GetAsync(externalTokenAddress);
            if (externalToken == null)
            {
                throw new ClientSideException(ExceptionType.WrongParams, $"External Token With address {externalTokenAddress} does not exist!");
            }

            BigInteger balance = await _ercInterfaceService.GetBalanceForExternalTokenAsync(ownerAddress, externalTokenAddress);

            return balance;
        }
    }
}
