using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelKickMessage
    {
        public enum ChannelKickResponseType
        {
            ChannelNotFound,
            PlayerNotFound,
            NoPermission,
        }

        public const string ChannelCreateMessageHashName = VChatPlugin.Name + ".ChannelKick";
        public const int Version = 1;

        static ChannelKickMessage()
        {
        }

        public static void Register()
        {
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelCreateMessageHashName, OnMessage_Server);
            }
        }

        private static void OnMessage_Server(long senderId, ZPackage package)
        {
            if (ZNet.m_isServer)
            {
                var version = package.ReadInt();
                var senderPeerId = package.ReadLong();
                var channelName = package.ReadString();
                var playerName = package.ReadString();

                // Only use the packet peer id if it's sent from the server.
                if (senderId != ValheimHelper.GetServerPeerId())
                {
                    senderPeerId = senderId;
                }

                if (ValheimHelper.GetSteamIdFromPeer(senderPeerId, out ulong senderSteamId))
                {
                    KickPlayerFromChannel(senderPeerId, senderSteamId, channelName, playerName);
                }
            }
        }

        public static void SendToPeer(long peerId, ChannelKickResponseType responseType, string channelName)
        {
            if (ZNet.m_isServer)
            {
                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    string text = null;
                    switch (responseType)
                    {
                        case ChannelKickResponseType.ChannelNotFound:
                            text = "Cannot find a channel with that name.";
                            break;
                        case ChannelKickResponseType.NoPermission:
                            text = "You do not have permission to remove a player from this channel.";
                            break;
                        default:
                            VChatPlugin.LogError($"Unknown response type for create channel received: {responseType}");
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
                VChatPlugin.LogWarning($"Cannot send the channel create response to a client.");
            }
        }

        public static void SendToServer(long senderPeerId, string channelName, string playerName)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(senderPeerId);
            package.Write(channelName);
            package.Write(playerName);

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelCreateMessageHashName, package);
        }

        private static bool KickPlayerFromChannel(long senderPeerId, ulong senderSteamId, string channelName, string playerName)
        {
            if (ZNet.m_isServer)
            {
                if (!ServerChannelManager.DoesChannelExist(channelName))
                {
                    SendToPeer(senderPeerId, ChannelKickResponseType.ChannelNotFound, channelName);
                }
                else
                {
                    var targetPeer = ZNet.instance.GetPeerByPlayerName(playerName);
                    if (targetPeer == null || !ValheimHelper.GetSteamIdFromPeer(targetPeer, out ulong targetSteamId))
                    {
                        SendToPeer(senderPeerId, ChannelKickResponseType.PlayerNotFound, channelName);
                    }
                    else if (ServerChannelManager.CanModerateChannel(senderSteamId, channelName))
                    {
                        return ServerChannelManager.RemovePlayerFromChannel(targetPeer.m_uid, targetSteamId, channelName);
                    }
                    else
                    {
                        SendToPeer(senderPeerId, ChannelKickResponseType.NoPermission, channelName);
                    }
                }
            }
            return false;
        }
    }
}
