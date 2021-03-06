using System;
using System.Collections.Generic;
using System.Linq;

namespace VChat.Data
{
    public class PluginCommand
    {
        public IEnumerable<string> CommandNames { get; private set; }
        public Action<string, Chat> Method { get; private set; }

        public PluginCommand(IEnumerable<string> commandNames, Action<string, object> method)
        {
            Method = method;
            CommandNames = commandNames
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();
        }
        public PluginCommand(string commandName, Action<string, object> method)
            : this(new[] {commandName}, method)
        {
        }
    }
}
