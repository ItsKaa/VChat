using System.Linq;
using UnityEngine;
using VChat.Extensions;
using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelChatMessage
    {
        public const int Version = 1;
        public const string ChannelChatMessageHashName = VChatPlugin.Name + ".ChannelChatMessage";


        public enum ChannelMessageResponseType
        {
            ChannelNotFound,
            NoPermission,
        }

        static ChannelChatMessage()
        {
        }

        public static void Register()
        {
            VChatPlugin.Log($"Registering custom routed messages for custom channel chat.");

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
            var colorHtmlString = package.ReadString();
            bool isSentByServer = senderId == ValheimHelper.GetServerPeerId();

            if (!isSentByServer)
            {
                peerId = senderId;
                var peer = ZNet.instance.GetPeer(peerId);
                callerName = peer?.m_playerName ?? callerName;
                pos = peer?.m_refPos ?? pos;
                colorHtmlString = null;
            }

            SendMessageToChannel(senderId, peerId, channelName, pos, callerName, text, colorHtmlString?.ToColor());
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
                    var customColorHtmlString = package.ReadString();

                    // Use the channel config to read the color or use the default.
                    var textColor = ChannelInfoMessage.FindChannel(channelName)?.Color ?? Color.white;

                    // Use custom color if provided
                    if (!string.IsNullOrEmpty(customColorHtmlString))
                    {
                        textColor = customColorHtmlString?.ToColor() ?? textColor;
                    }

                    var formattedMessage = VChatPlugin.GetFormattedMessage(textColor, channelName, callerName, text);
                    Chat.instance?.AddString(formattedMessage);
                }
                else
                {
                    VChatPlugin.LogWarning($"Ignoring a channel message from a client with id {senderId}.");
                }
            }
        }

        public static void SendToPeer(long peerId, string channelName, Vector3 pos, string callerName, string text, Color? customColor = null)
        {
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write(channelName ?? string.Empty);
                package.Write(pos);
                package.Write(callerName ?? string.Empty);
                package.Write(text ?? string.Empty);
                package.Write(customColor?.ToHtmlString() ?? string.Empty);

                MessageHelper.SendMessageToPeer(peerId, channelName, callerName, text, ChannelChatMessageHashName, package, new System.Version(2,0,0), customColor);
            }
            else
            {
                VChatPlugin.LogWarning($"Clients cannot send a chat message directly to another peer.");
            }
        }

        private static void SendResponseToPeer(long peerId, ChannelMessageResponseType responseType, string channelName)
        {
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write((int)responseType);
                package.Write(channelName);

                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    string text = null;
                    switch (responseType)
                    {
                        case ChannelMessageResponseType.ChannelNotFound:
                            text = "Cannot find a channel with that name.";
                            break;
                        case ChannelMessageResponseType.NoPermission:
                            text = "You do not have permission to send a message to that channel.";
                            break;
                        default:
                            VChatPlugin.LogError($"Unknown response type for disband channel received: {responseType}");
                            break;
                    }

                    VChatPlugin.Log($"[Chat Message] Sending response {responseType} to peer {peerId} for channel {channelName}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel message response to a client.");
            }
        }

        public static void SendToServer(long senderPeerId, string channelName, Vector3 pos, string callerName, string text, Color? customColor = null)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(senderPeerId);
            package.Write(channelName ?? string.Empty);
            package.Write(pos);
            package.Write(callerName ?? string.Empty);
            package.Write(text ?? string.Empty);
            package.Write(customColor?.ToHtmlString() ?? string.Empty);

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelChatMessageHashName, package);
        }

        private static bool SendMessageToChannel(long senderPeerId, long peerId, string channelName, Vector3 pos, string callerName, string text, Color? color)
        {
            var channel = ServerChannelManager.FindChannel(channelName);

            if (senderPeerId == ValheimHelper.GetServerPeerId() && peerId == senderPeerId)
            {
                // Server ignores every check, channel is verified again in this method.
                VChatPlugin.Log($"Sending message as server to channel {channelName}");
                return ServerChannelManager.SendMessageToAllPeersInChannel(channelName, callerName, text, color);
            }
            else if (channel == null)
            {
                SendResponseToPeer(peerId, ChannelMessageResponseType.ChannelNotFound, channel.Name);
            }
            else if (!ValheimHelper.GetSteamIdFromPeer(peerId, out ulong steamId))
            {
                VChatPlugin.LogError($"Peer {peerId} sent a message to the channel {channelName} but steam id cannot not be found.");
            }
            else if (!channel.GetSteamIds().Contains(steamId) && !channel.IsEveryoneConnected && !channel.IsNotificationChannel)
            {
                SendResponseToPeer(peerId, ChannelMessageResponseType.NoPermission, channel.Name);
            }
            else
            {
                VChatPlugin.Log($"Sending message as user: [{channelName}] {callerName}: {text}");
                return ServerChannelManager.SendMessageToAllUsersInChannelForPeer(peerId, channel.Name, callerName, text, color);
            }

            return false;
        }
    }
}
