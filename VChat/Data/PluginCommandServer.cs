using System;
using System.Collections.Generic;

namespace VChat.Data
{
    public class PluginCommandServer : PluginCommandBase
    {
        public Action<string, long, ulong> Method { get; private set; }

        public PluginCommandServer(PluginCommandType type, IEnumerable<string> commandNames, Action<string, long, ulong> method)
            : base(type, commandNames)
        {
            Method = method;
        }

        public PluginCommandServer(IEnumerable<string> commandNames, Action<string, long, ulong> method)
            : this(PluginCommandType.None, commandNames, method)
        {
        }

        public PluginCommandServer(PluginCommandType type, string commandName, Action<string, long, ulong> method)
            : this(type, new[] { commandName }, method)
        {
        }

        public PluginCommandServer(string commandName, Action<string, long, ulong> method)
            : this(PluginCommandType.None, new[] { commandName }, method)
        {
        }
    }
}
