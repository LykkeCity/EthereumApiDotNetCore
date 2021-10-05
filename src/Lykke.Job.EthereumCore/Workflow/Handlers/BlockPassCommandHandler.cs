using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.EthereumCore.Workflow.Commands;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.PassToken;
using System.Threading.Tasks;

namespace Lykke.Job.EthereumCore.Workflow.Handlers
{
    public class BlockPassCommandHandler
    {
        private readonly IBlockPassService _blockPassService;
        private readonly ILog _logger;

        public BlockPassCommandHandler(
            IBlockPassService blockPassService,
            ILog logger)
        {
            _blockPassService = blockPassService;
            _logger = logger;
        }

        public async Task<CommandHandlingResult> Handle(AddToPassWhiteListCommand command, IEventPublisher eventPublisher)
        {
            try
            {
                _logger.WriteInfo(nameof(BlockPassCommandHandler), command, "Adding address to whitelist");
                string ticketId = await _blockPassService.AddToWhiteListAsync(command.Address);
            }
            catch (ClientSideException ex)
            {
                if (ex.ExceptionType == ExceptionType.EntityAlreadyExists ||
                    ex.ExceptionType == ExceptionType.OperationWithIdAlreadyExists)
                {
                    _logger.WriteWarning(nameof(BlockPassCommandHandler),
                        command,
                        $"Address passed to BlockPass already, {command.Address}", ex);
                }
                else
                {
                    _logger.WriteError(nameof(BlockPassCommandHandler),
                        command,
                        ex);

                    throw;
                }
            }

            return CommandHandlingResult.Ok();
        }
    }
}
