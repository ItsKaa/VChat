using System;
using System.Collections.Generic;
using VChat.Data.Messages;

namespace VChat.Data
{
    public class PluginCommandServerChannel : PluginCommandServer
    {
        public ServerChannelInfo ChannelInfo { get; private set; } = null;

        public PluginCommandServerChannel(PluginCommandType type, IEnumerable<string> commandNames, ServerChannelInfo channelInfo, Action<string, long, ulong> method)
            : base(type, commandNames, method)
        {
            ChannelInfo = channelInfo;
        }

        public PluginCommandServerChannel(IEnumerable<string> commandNames, ServerChannelInfo channelInfo, Action<string, long, ulong> method)
            : this(PluginCommandType.None, commandNames, channelInfo, method)
        {
        }

        public PluginCommandServerChannel(PluginCommandType type, string commandName, ServerChannelInfo channelInfo, Action<string, long, ulong> method)
            : this(type, new[] { commandName }, channelInfo, method)
        {
        }

        public PluginCommandServerChannel(string commandName, ServerChannelInfo channelInfo, Action<string, long, ulong> method)
            : this(PluginCommandType.None, new[] { commandName }, channelInfo, method)
        {
        }
    }
}
