using System;
using System.Collections.Generic;

namespace VChat.Data
{
    public class PluginCommandClient : PluginCommandBase
    {
        public Action<string, object> Method { get; private set; }

        public PluginCommandClient(PluginCommandType type, IEnumerable<string> commandNames, Action<string, object> method)
            : base(type, commandNames)
        {
            Method = method;
        }

        public PluginCommandClient(IEnumerable<string> commandNames, Action<string, object> method)
            : this(PluginCommandType.None, commandNames, method)
        {
        }

        public PluginCommandClient(PluginCommandType type, string commandName, Action<string, object> method)
            : this(type, new[] {commandName}, method)
        {
        }

        public PluginCommandClient(string commandName, Action<string, object> method)
            : this(PluginCommandType.None, new[] { commandName }, method)
        {
        }
    }
}
