using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelCreateMessage
    {
        public enum ChannelCreateResponseType
        {
            ChannelAlreadyExists,
            NoPermission,
        }

        public const string ChannelCreateMessageHashName = VChatPlugin.Name + ".ChannelCreate";
        public const int Version = 1;

        static ChannelCreateMessage()
        {
        }

        public static void Register()
        {
            if (ZNet.m_isServer)
            {
                VChatPlugin.Log($"Registering custom routed messages for channel create messages.");

                ZRoutedRpc.instance.Register<ZPackage>(ChannelCreateMessageHashName, OnMessage_Server);
            }
        }

        private static void OnMessage_Server(long senderId, ZPackage package)
        {
            if (ZNet.m_isServer)
            {
                var version = package.ReadInt();
                var channelName = package.ReadString();
                var senderPeerId = package.ReadLong();

                // Only use the packet peer id if it's sent from the server.
                if (senderId != ValheimHelper.GetServerPeerId())
                {
                    senderPeerId = senderId;
                }

                var senderPeer = ZNet.instance?.GetPeer(senderPeerId);
                if (ValheimHelper.GetSteamIdFromPeer(senderPeer, out ulong senderSteamId))
                {
                    VChatPlugin.Log($"Player \"{senderPeer.m_playerName}\" ({senderPeerId}) requested to create channel named {channelName}.");
                    AddChannelForPeer(senderPeerId, senderSteamId, channelName);
                }
            }
        }

        public static void SendToPeer(long peerId, ChannelCreateResponseType responseType, string channelName)
        {
            if (ZNet.m_isServer)
            {
                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    string text = null;
                    switch(responseType)
                    {
                        case ChannelCreateResponseType.ChannelAlreadyExists:
                            text = "A channel with that name already exists.";
                            break;
                        case ChannelCreateResponseType.NoPermission:
                            text = "You do not have permission to create a channel.";
                            break;
                        default:
                            VChatPlugin.LogError($"Unknown response type for create channel received: {responseType}");
                            break;
                    }

                    VChatPlugin.Log($"[Create Channel] Sending response {responseType} to peer {peerId} for channel {channelName}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel create response to a client.");
            }
        }

        public static void SendToServer(long senderPeerId, string channelName)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(channelName);
            package.Write(senderPeerId);

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelCreateMessageHashName, package);
        }

        /// <summary>
        /// Adds a channel for the provided peer, this has to be executed by the server.
        /// </summary>
        private static bool AddChannelForPeer(long peerId, ulong steamId, string channelName)
        {
            if (ZNet.m_isServer)
            {
                if (ServerChannelManager.DoesChannelExist(channelName) || string.IsNullOrWhiteSpace(channelName))
                {
                    SendToPeer(peerId, ChannelCreateResponseType.ChannelAlreadyExists, channelName);
                }
                else if (!ServerChannelManager.CanUsersCreateChannels && !ValheimHelper.IsAdministrator(steamId))
                {
                    SendToPeer(peerId, ChannelCreateResponseType.NoPermission, channelName);
                }
                else
                {
                    return ServerChannelManager.AddChannel(channelName, steamId, false);
                }
            }

            return false;
        }
    }
}
