using UnityEngine;
using VChat.Helpers;

namespace VChat.Messages
{
    public static class ChannelChatMessage
    {
        public const int Version = 1;
        public const string ChannelChatMessageHashName = VChatPlugin.Name + ".ChannelChatMessage";

        static ChannelChatMessage()
        {
        }

        public static void Register()
        {
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelChatMessageHashName, OnMessage_Server);
            }
            else
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelChatMessageHashName, OnMessage_Client);
            }
        }

        private static void OnMessage_Server(long senderId, ZPackage package)
        {
            var version = package.ReadInt();
            var peerId = package.ReadLong();
            var channelName = package.ReadString();
            Vector3 pos = package.ReadVector3();
            var callerName = package.ReadString();
            var text = package.ReadString();

            if (senderId != ValheimHelper.GetServerPeerId())
            {
                peerId = senderId;
                var peer = ZNet.instance.GetPeer(peerId);
                callerName = peer?.m_playerName ?? callerName;
                pos = peer?.m_refPos ?? pos;
            }

            MessageHelper.SendMessageToPeer(peerId, channelName, callerName, text, () =>
            {
                SendToPeer(peerId, channelName, pos, callerName, text);
            });
        }

        private static void OnMessage_Client(long senderId, ZPackage package)
        {
            if (!ZNet.m_isServer)
            {
                if (senderId == ValheimHelper.GetServerPeerId())
                {
                    var version = package.ReadInt();
                    var channelName = package.ReadString();
                    Vector3 pos = package.ReadVector3();
                    var callerName = package.ReadString();
                    var text = package.ReadString();

                    // Use the channel config to read the color or use the default.
                    var textColor = ChannelInfoMessage.FindChannel(channelName)?.Color ?? Color.white;
                    var formattedMessage = VChatPlugin.GetFormattedMessage(textColor, channelName, callerName, text);
                    Chat.instance?.AddString(formattedMessage);
                }
                else
                {
                    VChatPlugin.LogWarning($"Ignoring a channel message from a client with id {senderId}.");
                }
            }
        }

        public static void SendToPeer(long peerId, string channelName, Vector3 pos, string callerName, string text)
        {
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write(channelName ?? string.Empty);
                package.Write(pos);
                package.Write(callerName ?? string.Empty);
                package.Write(text ?? string.Empty);

                MessageHelper.SendMessageToPeer(peerId, channelName, callerName, text, ChannelChatMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Clients cannot send a chat message directly to another peer.");
            }
        }

        public static void SendToServer(long senderPeerId, string channelName, Vector3 pos, string callerName, string text)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(senderPeerId);
            package.Write(channelName ?? string.Empty);
            package.Write(pos);
            package.Write(callerName ?? string.Empty);
            package.Write(text ?? string.Empty);

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelChatMessageHashName, package);
        }
    }
}
