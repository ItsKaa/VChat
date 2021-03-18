using System.Collections.Generic;
using System.Linq;
using VChat.Data.Messages;
using VChat.Patches;

namespace VChat.Messages
{
    public static class ChannelInfoMessage
    {
        public const string ChannelInfoMessageHashName = VChatPlugin.Name + ".ChannelInfo";
        public const int Version = 1;

        static ChannelInfoMessage()
        {
        }

        public static void Register()
        {
            if (!ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelInfoMessageHashName, OnMessage_Client);
            }
        }

        private static void OnMessage_Client(long senderId, ZPackage package)
        {
            if (senderId == ZNet.instance.GetServerPeer()?.m_uid)
            {
                var version = package.ReadInt();
                var packageCount = package.ReadInt();
                VChatPlugin.LogWarning($"Received a channel package from the server with {packageCount} channels.");
                for (int i = 0; i < packageCount; i++)
                {
                    var channelPackage = package.ReadPackage();
                    var channelName = channelPackage.ReadString();
                    var ownerId = channelPackage.ReadULong();
                    var isPublic = channelPackage.ReadBool();
                    VChatPlugin.LogWarning($"Received a channel configuration from the server ({senderId}), channel name: {channelName}, owner ID: {ownerId}, public: {isPublic}");
                }
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
                    channelDataPackage.Write(channelInfo.Name);
                    channelDataPackage.Write(channelInfo.OwnerId);
                    channelDataPackage.Write(channelInfo.IsPublic);
                    package.Write(channelDataPackage);
                }

                VChatPlugin.LogWarning($"Sending channel pacakge to {peerId} with {channelInfoData.Count()} channels.");
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ChannelInfoMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the greeing to a client.");
            }
        }
    }
}
