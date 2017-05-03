using Common.Log;
using Core;
using Core.Notifiers;
using Core.Repositories;
using Core.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class ExternalTokenService
    {
        private readonly IContractService _contractService;
        private readonly ILog _logger;
        private readonly IBaseSettings _settings;
        private readonly IExternalTokenRepository _tokenRepository;

        public ExternalTokenService(IExternalTokenRepository tokenRepository,
            IContractService contractService,
            ILog logger,
            IBaseSettings settings)
        {
            _contractService = contractService;
            _tokenRepository = tokenRepository;
            _logger = logger;
            _settings = settings;
        }

        //Lykke is the owner of that token
        public async Task<IExternalToken> CreateExternalToken(string name)
        {
            string id = Guid.NewGuid().ToString();
            string contractAddress = await _contractService.CreateContract(_settings.ExternalTokenContract.Abi, 
                _settings.ExternalTokenContract.ByteCode, 
                _settings.EthereumMainAccount);

            IExternalToken externalToken = new ExternalToken()
            {
                Id = id,
                Name = name,
                ContractAddress = contractAddress,
            };

            await _tokenRepository.SaveAsync(externalToken);

            return externalToken;
        }
    }
}
