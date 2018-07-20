using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.EthereumCore.Contracts.Cqrs.Commands;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.HotWallet;
using Nethereum.Util;

namespace Lykke.Job.EthereumCore.Workflow.Handlers
{
    public class CashoutCommandHandler
    {
        private readonly IAssetsService _assetsService;
        private readonly IHotWalletService _hotWalletService;
        private readonly ILog _logger;
        private readonly IPendingOperationService _pendingOperationService;
        private readonly AddressUtil _addressUtil;

        public CashoutCommandHandler(
            IAssetsService assetsService, 
            IHotWalletService hotWalletService, 
            ILog logger, 
            IPendingOperationService pendingOperationService)
        {
            _assetsService = assetsService;
            _hotWalletService = hotWalletService;
            _logger = logger;
            _pendingOperationService = pendingOperationService;
            _addressUtil = new AddressUtil();
        }

        public async Task<CommandHandlingResult> Handle(StartCashoutCommand command, IEventPublisher eventPublisher)
        {
            var asset = await _assetsService.AssetGetAsync(command.AssetId);
            var amount = EthServiceHelpers.ConvertToContract(command.Amount, asset.MultiplierPower, asset.Accuracy);

            try
            {
                if (asset.Type == AssetType.Erc20Token)
                {
                    var token = await _assetsService.Erc20TokenGetBySpecificationAsync(new Erc20TokenSpecification(new List<string>() { asset.Id }));

                    var tokenAddress = token?.Items?.FirstOrDefault()?.Address;

                    if (string.IsNullOrEmpty(tokenAddress))
                    {
                        _logger.WriteWarning(nameof(CashoutCommandHandler), nameof(Handle), $"Can't perform cashout on empty token, {command.Id}");
                        return CommandHandlingResult.Ok();
                    }

                    await _hotWalletService.EnqueueCashoutAsync(new Service.EthereumCore.Core.Repositories.HotWalletOperation()
                    {
                        Amount = amount,
                        OperationId = command.Id.ToString(),
                        FromAddress = command.FromAddress,
                        ToAddress = command.ToAddress,
                        TokenAddress = tokenAddress
                    });
                }
                else
                {
                    await _pendingOperationService.CashOut(command.Id, asset.AssetAddress,
                        _addressUtil.ConvertToChecksumAddress(command.FromAddress), _addressUtil.ConvertToChecksumAddress(command.ToAddress), amount, string.Empty);
                }
            }
            catch (ClientSideException ex) when (ex.ExceptionType == ExceptionType.EntityAlreadyExists || ex.ExceptionType == ExceptionType.OperationWithIdAlreadyExists)
            {
                _logger.WriteWarning(nameof(CashoutCommandHandler), nameof(Handle), $"Operation already exists, {command.Id}", ex);
            }

            return CommandHandlingResult.Ok();
        }
    }
}
