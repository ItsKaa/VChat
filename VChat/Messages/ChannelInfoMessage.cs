using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VChat.Data.Messages;
using VChat.Extensions;
using VChat.Helpers;

namespace VChat.Messages
{
    public static class ChannelInfoMessage
    {
        public const string ChannelInfoMessageHashName = VChatPlugin.Name + ".ChannelInfo";
        public const int Version = 1;

        private static List<ServerChannelInfo> ReceivedChannelInfo { get; set; }
        private static object _lock = new object();

        static ChannelInfoMessage()
        {
            ReceivedChannelInfo = new List<ServerChannelInfo>();
        }

        public static void Register()
        {
            if (!ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelInfoMessageHashName, OnMessage_Client);
            }
        }

        public static IEnumerable<ServerChannelInfo> GetChannelInfo()
        {
            lock (_lock)
            {
                return ReceivedChannelInfo.ToList();
            }
        }

        internal static ServerChannelInfo FindChannel(IEnumerable<ServerChannelInfo> collection, string channelName)
        {
            return collection.FirstOrDefault(x => ValheimHelper.NameEquals(x.Name, channelName));
        }

        internal static ServerChannelInfo FindChannel(string channelName)
        {
            lock (_lock)
            {
                return FindChannel(ReceivedChannelInfo, channelName);
            }
        }

        private static void OnMessage_Client(long senderId, ZPackage package)
        {
            if (senderId == ValheimHelper.GetServerPeerId())
            {
                var version = package.ReadInt();
                var packageCount = package.ReadInt();
                VChatPlugin.Log($"Received a channel package from the server with {packageCount} channels.");

                var list = new List<ServerChannelInfo>();
                for (int i = 0; i < packageCount; i++)
                {
                    var channelPackage = package.ReadPackage();
                    var channelName = channelPackage.ReadString();
                    var colorString = channelPackage.ReadString();
                    var ownerId = channelPackage.ReadULong();
                    var isPublic = channelPackage.ReadBool();
                    var isReadOnly = channelPackage.ReadBool();
                    list.Add(new ServerChannelInfo()
                    {
                        Color = colorString.ToColor() ?? Color.white,
                        IsPublic = isPublic,
                        Name = channelName,
                        OwnerId = ownerId,
                        IsPluginOwnedChannel = isReadOnly,
                    });
                }

                lock (_lock)
                {
                    // Remove deleted channels
                    foreach (var channel in ReceivedChannelInfo.ToList())
                    {
                        if (!list.Exists(x => ValheimHelper.NameEquals(x.Name, channel.Name)))
                        {
                            VChatPlugin.Log($"Removed channel with name '{channel.Name}'");

                            var existingChannel = FindChannel(channel.Name);
                            ReceivedChannelInfo.Remove(existingChannel);
                        }
                    }

                    // Add or update channels
                    foreach (var channel in list)
                    {
                        var existingChannel = FindChannel(channel.Name);
                        if (existingChannel != null)
                        {
                            VChatPlugin.Log($"Updated channel configuration - channel name: {channel.Name}, command name: {channel.ServerCommandName}, color: {channel.Color}, owner ID: {channel.OwnerId}, public: {channel.IsPublic}, read-only: {channel.IsPluginOwnedChannel}");
                            existingChannel.Update(channel);
                        }
                        else
                        {
                            VChatPlugin.Log($"Received a channel configuration from the server - channel name: {channel.Name}, command name: {channel.ServerCommandName}, color: {channel.Color}, owner ID: {channel.OwnerId}, public: {channel.IsPublic}, read-only: {channel.IsPluginOwnedChannel}");
                            ReceivedChannelInfo.Add(channel);
                        }
                    }
                }

                // Re-initialise server commands
                VChatPlugin.InitialiseServerCommands();
            }
            else
            {
                VChatPlugin.LogWarning($"Ignoring a channel configuration message received from a client with id {senderId}.");
            }
        }

        public static void SendToPeer(long peerId, IEnumerable<ServerChannelInfo> channelInfoData)
        {
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write(channelInfoData.Count());

                foreach (var channelInfo in channelInfoData)
                {
                    var channelDataPackage = new ZPackage();
                    channelDataPackage.Write(channelInfo.Name ?? string.Empty);
                    channelDataPackage.Write(channelInfo.Color.ToHtmlString());
                    channelDataPackage.Write(channelInfo.OwnerId);
                    channelDataPackage.Write(channelInfo.IsPublic);
                    channelDataPackage.Write(channelInfo.IsPluginOwnedChannel);
                    package.Write(channelDataPackage);
                }

                VChatPlugin.Log($"Sending channel pacakge to {peerId} with {channelInfoData.Count()} channels.");
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ChannelInfoMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the greeing to a client.");
            }
        }
    }
}
