using UnityEngine;
using VChat.Extensions;
using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelEditMessage
    {
        public enum ChannelEditType
        {
            EditChannelColor,
        }

        public enum ChannelEditResponseType
        {
            OK,
            ChannelNotFound,
            NoPermission,
            InvalidValue,
        }

        public const string ChannelEditMessageHashName = VChatPlugin.Name + ".ChannelEdit";
        public const int Version = 1;

        static ChannelEditMessage()
        {
        }

        public static void Register()
        {
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelEditMessageHashName, OnMessage_Client);
            }
        }

        private static void OnMessage_Client(long senderId, ZPackage package)
        {
            var version = package.ReadInt();
            var senderPeerId = package.ReadLong();
            var editType = package.ReadInt();
            var channelName = package.ReadString();
            var value = package.ReadString();

            // Only use the packet peer id if it's sent from the server.
            if (senderId != ValheimHelper.GetServerPeerId())
            {
                senderPeerId = senderId;
            }

            if (ValheimHelper.GetSteamIdFromPeer(senderPeerId, out ulong senderSteamId))
            {
                if (editType == (int)ChannelEditType.EditChannelColor)
                {
                    EditChannelColor(senderPeerId, senderSteamId, channelName, value);
                }
            }
        }

        public static void SendToPeer(long peerId, ChannelEditType type, ChannelEditResponseType responseType, string channelName, string value)
        {
            if (ZNet.m_isServer)
            {
                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    string text = null;
                    switch (responseType)
                    {
                        case ChannelEditResponseType.ChannelNotFound:
                            text = "Cannot find a channel with that name.";
                            break;
                        case ChannelEditResponseType.NoPermission:
                            text = "You do not have permission to edit that channel.";
                            break;
                        case ChannelEditResponseType.InvalidValue:
                            {
                                if (type == ChannelEditType.EditChannelColor)
                                {
                                    text = $"Could not parse the color '{value}'";
                                }
                                else
                                {
                                    VChatPlugin.LogError($"Unknown type for edit channel - InvalidValue received: {responseType}");
                                }
                            }
                            break;
                        default:
                            VChatPlugin.LogError($"Unknown response type for edit channel received: {responseType}");
                            break;
                    }

                    VChatPlugin.Log($"[Channel Edit] Sending response {type}:{responseType} to peer {peerId} for channel {channelName}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel edit response to a client.");
            }
        }

        public static void SendToServer(long senderId, ChannelEditType type, string channelName, string value)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(senderId);
            package.Write((int)type);
            package.Write(channelName ?? string.Empty);
            package.Write(value ?? string.Empty);

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelEditMessageHashName, package);
        }
        public static void SendToServer(long senderId, ChannelEditType type, string channelName, Color color)
            => SendToServer(senderId, type, channelName, color.ToHtmlString());

        private static bool EditChannelColor(long peerId, ulong steamId, string channelName, string colorValue)
        {
            var color = colorValue?.ToColor();
            if (!ServerChannelManager.DoesChannelExist(channelName))
            {
                SendToPeer(peerId, ChannelEditType.EditChannelColor, ChannelEditResponseType.ChannelNotFound, channelName, colorValue);
            }
            else if (!color.HasValue)
            {
                SendToPeer(peerId, ChannelEditType.EditChannelColor, ChannelEditResponseType.InvalidValue, channelName, colorValue);
            }
            else if(!ServerChannelManager.CanModerateChannel(steamId, channelName))
            {
                SendToPeer(peerId, ChannelEditType.EditChannelColor, ChannelEditResponseType.NoPermission, channelName, colorValue);
            }
            else
            {
                return ServerChannelManager.EditChannelColor(channelName, color.Value, peerId);
            }

            return false;
        }
    }
}
