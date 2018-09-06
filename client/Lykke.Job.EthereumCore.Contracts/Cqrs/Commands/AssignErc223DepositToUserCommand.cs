namespace Lykke.Job.EthereumCore.Workflow.Commands
{
    public class AssignErc223DepositToUserCommand
    {
        public string UserAddress { get; set; }

        public string ContractAddress { get; set; }
    }
}
