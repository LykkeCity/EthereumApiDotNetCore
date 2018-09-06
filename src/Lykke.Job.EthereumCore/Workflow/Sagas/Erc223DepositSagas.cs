using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.EthereumCore.Contracts.Cqrs;
using Lykke.Job.EthereumCore.Contracts.Cqrs.Events;
using Lykke.Job.EthereumCore.Workflow.Commands;

namespace Lykke.Job.EthereumCore.Workflow.Sagas
{
    public class Erc223DepositSagas
    {
        public Erc223DepositSagas(ILog logger)
        {
        }

        [UsedImplicitly]
        public async Task Handle(Erc223DepositAssignedToUserEvent evt, ICommandSender commandSender)
        {
            var command = new AddToPassWhiteListCommand()
            {
                Address = evt.ContractAddress
            };

            commandSender.SendCommand(command, EthereumBoundedContext.Name);
        }
    }
}
