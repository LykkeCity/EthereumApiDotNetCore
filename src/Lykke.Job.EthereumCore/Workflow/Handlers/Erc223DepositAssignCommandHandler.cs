using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.EthereumCore.Contracts.Cqrs.Events;
using Lykke.Job.EthereumCore.Workflow.Commands;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.PassToken;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services;

namespace Lykke.Job.EthereumCore.Workflow.Handlers
{
    public class Erc223DepositAssignCommandHandler
    {
        private readonly IErc20DepositContractService _contractService;
        private readonly IErc223DepositContractRepository _contractRepository;
        private readonly ILog _logger;

        public Erc223DepositAssignCommandHandler(
            [KeyFilter(Constants.DefaultKey)] IErc223DepositContractRepository contractRepository,
            IErc20DepositContractQueueServiceFactory poolFactory,
            ILog logger)
        {
            _contractRepository = contractRepository;
            //_contractService = contractService;
            _logger = logger;
        }

        public async Task<CommandHandlingResult> Handle(AssignErc223DepositToUserCommand command,
            IEventPublisher eventPublisher)
        {
            await _contractRepository.AddOrReplace(new Erc20DepositContract
            {
                ContractAddress = command.ContractAddress,
                UserAddress = command.UserAddress
            });

            eventPublisher.PublishEvent(new Erc223DepositAssignedToUserEvent()
            {
                UserAddress = command.UserAddress,
                ContractAddress = command.ContractAddress
            });

            return CommandHandlingResult.Ok();
        }
    }
}