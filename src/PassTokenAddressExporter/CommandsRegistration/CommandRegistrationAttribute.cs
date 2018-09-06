using System;

namespace PassTokenAddressExporter.CommandsRegistration
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandRegistrationAttribute : Attribute
    {
        public CommandRegistrationAttribute(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; }
    }
}
