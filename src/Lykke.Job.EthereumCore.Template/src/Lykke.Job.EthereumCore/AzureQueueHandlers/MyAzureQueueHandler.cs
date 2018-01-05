using System.Threading.Tasks;
using Lykke.Job.EthereumCore.IncomingMessages;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Lykke.Job.EthereumCore.AzureQueueHandlers
{
    // NOTE: This is the azure queue handlers class example.
    // All handlers are founded and added to the DI container by JobTriggers infrastructure, 
    // when you call builder.AddTriggers() in JobModule. Further, JobTriggers infrastructure manages handlers execution.

    // TODO: Add as many queue handler classes as necessary to logicaly group all HandleXXXMessage methods

    public class MyAzureQueueHandler
    {
        // NOTE: The object is instantiated using DI container, so registered dependencies are injects well
        public MyAzureQueueHandler()
        {
        }

        // TODO: Add your message handling methods here, specifying queue name in the QueueTrigger attribiute:
        
        [QueueTrigger("queue-name")]
        public async Task HandleMyMessage(MySubscribedMessage msg)
        {
            // TODO: Orchestrate execution flow here and delegate actual business logic implementation to services layer
            // Do not implement actual business logic here

            await Task.CompletedTask;
        }
    }
}   