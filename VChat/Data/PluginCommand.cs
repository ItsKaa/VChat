using System;
using System.Collections.Generic;
using System.Linq;

namespace VChat.Data
{
    public class PluginCommand
    {
        public PluginCommandType Type { get; private set; }
        public IEnumerable<string> CommandNames { get; private set; }
        public Action<string, Chat> Method { get; private set; }

        public PluginCommand(PluginCommandType type, IEnumerable<string> commandNames, Action<string, object> method)
        {
            Type = type;
            Method = method;
            CommandNames = commandNames
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();
        }
        public PluginCommand(PluginCommandType type, string commandName, Action<string, object> method)
            : this(type, new[] {commandName}, method)
        {
        }
    }
}
