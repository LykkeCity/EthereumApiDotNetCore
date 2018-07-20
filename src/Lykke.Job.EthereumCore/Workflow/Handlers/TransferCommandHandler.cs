using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.EthereumCore.Contracts.Cqrs.Commands;
using Lykke.Service.Assets.Client;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Services;
using Nethereum.Util;

namespace Lykke.Job.EthereumCore.Workflow.Handlers
{
    public class TransferCommandHandler
    {
        private readonly IAssetsService _assetsService;
        private readonly ILog _logger;
        private readonly IPendingOperationService _pendingOperationService;
        private readonly AddressUtil _addressUtil;

        public TransferCommandHandler(
            IAssetsService assetsService,
            ILog logger,
            IPendingOperationService pendingOperationService)
        {
            _assetsService = assetsService;
            _logger = logger;
            _pendingOperationService = pendingOperationService;
            _addressUtil = new AddressUtil();
        }

        public async Task<CommandHandlingResult> Handle(StartTransferCommand command, IEventPublisher eventPublisher)
        {
            var asset = await _assetsService.AssetGetAsync(command.AssetId);
            var amount = EthServiceHelpers.ConvertToContract(command.Amount, asset.MultiplierPower, asset.Accuracy);

            try
            {
                await _pendingOperationService.Transfer(command.Id, asset.AssetAddress,
                    _addressUtil.ConvertToChecksumAddress(command.FromAddress), _addressUtil.ConvertToChecksumAddress(command.ToAddress), amount, command.Sign);
            }
            catch (ClientSideException ex) when (ex.ExceptionType == ExceptionType.EntityAlreadyExists || ex.ExceptionType == ExceptionType.OperationWithIdAlreadyExists)
            {
                _logger.WriteWarning(nameof(TransferCommandHandler), nameof(Handle), $"Operation already exists, {command.Id}", ex);
            }

            return CommandHandlingResult.Ok();
        }
    }
}
