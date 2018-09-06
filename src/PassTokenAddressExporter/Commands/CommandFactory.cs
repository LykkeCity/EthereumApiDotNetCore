using System;
using PassTokenAddressExporter.Helpers;

namespace PassTokenAddressExporter.Commands
{
    public class CommandFactory
    {
        private readonly IConfigurationHelper _helper;

        public CommandFactory(IConfigurationHelper helper)
        {
            _helper = helper;
        }

        public ICommand CreateCommand(Func<IConfigurationHelper, ICommand> createFunc)
        {
            var command = createFunc(_helper);

            return command;
        }
    }
}
