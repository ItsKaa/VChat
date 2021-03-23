using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelEditMessage
    {
        public enum ChannelEditType
        {
            ServerResponse,
            EditChannelName,
            EditChannelCommand,
            EditChannelColor,
            EditChannelOwner,
            EditChannelIsPublic,
        }

        public enum ChannelEditResponseType
        {
            ChannelNotFound,
            NoPermission,
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
            if (senderId != ValheimHelper.GetServerPeerId())
            {
                var peer = ZNet.instance.GetPeer(senderId);
                if (peer != null && peer.m_socket is ZSteamSocket steamSocket)
                {
                    var version = package.ReadInt();
                    var editType = package.ReadInt();
                    var channelName = package.ReadString();
                    var value = package.ReadString();
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Ignoring a channel edit message received from a client with id {senderId}.");
            }
        }

        public static void SendResponseToPeer(long peerId, ChannelEditResponseType responseType, string channelName)
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
                        default:
                            VChatPlugin.LogError($"Unknown response type for edit channel received: {responseType}");
                            break;
                    }

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

        public static void SendRequestToServer(ChannelEditType type, string channelName, string value)
        {
            if (!ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write((int)type);
                package.Write(channelName);
                package.Write(value);

                ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelEditMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel edit request to a client.");
            }
        }
    }
}
