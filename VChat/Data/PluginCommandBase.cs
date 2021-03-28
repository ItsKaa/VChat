using System.Collections.Generic;
using System.Linq;

namespace VChat.Data
{
    public abstract class PluginCommandBase
    {
        public PluginCommandType Type { get; private set; }
        public IEnumerable<string> CommandNames { get; private set; }

        public PluginCommandBase(PluginCommandType type, IEnumerable<string> commandNames)
        {
            Type = type;
            CommandNames = commandNames
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();
        }

        public PluginCommandBase(IEnumerable<string> commandNames)
            : this(PluginCommandType.None, commandNames)
        {
        }

        public PluginCommandBase(PluginCommandType type, string commandName)
            : this(type, new[] { commandName })
        {
        }

        public PluginCommandBase(string commandName)
            : this(PluginCommandType.None, new[] { commandName })
        {
        }
    }
}
