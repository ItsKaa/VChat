using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelDisbandMessage
    {
        public enum ChannelDisbandResponseType
        {
            ChannelNotFound,
            NoPermission,
        }

        public const string ChannelDisbandMessageHashName = VChatPlugin.Name + ".ChannelDisband";
        public const int Version = 1;

        static ChannelDisbandMessage()
        {
        }

        public static void Register()
        {
            if (ZNet.m_isServer)
            {
                VChatPlugin.Log($"Registering custom routed messages for channel disband.");

                ZRoutedRpc.instance.Register<ZPackage>(ChannelDisbandMessageHashName, OnMessage_Server);
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

                var peer = ZNet.instance?.GetPeer(senderPeerId);
                if(ValheimHelper.GetSteamIdFromPeer(peer, out ulong steamId))
                {
                    VChatPlugin.LogWarning($"Player \"{peer.m_playerName}\" ({senderPeerId}) requested to disband channel named {channelName}.");
                    DisbandChannelForPeer(senderPeerId, steamId, channelName);
                }
            }
        }

        public static void SendToPeer(long peerId, ChannelDisbandResponseType responseType, string channelName)
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
                        case ChannelDisbandResponseType.ChannelNotFound:
                            text = "Cannot find a channel with that name.";
                            break;
                        case ChannelDisbandResponseType.NoPermission:
                            text = "You do not have permission to disband that channel.";
                            break;
                        default:
                            VChatPlugin.LogError($"Unknown response type for disband channel received: {responseType}");
                            break;
                    }

                    VChatPlugin.Log($"[Channel Disband] Sending response {responseType} to peer {peerId} for channel {channelName}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel disband response to a client.");
            }
        }

        public static void SendToServer(long senderPeerId, string channelName)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(channelName);
            package.Write(senderPeerId);

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelDisbandMessageHashName, package);
        }

        /// <summary>
        /// Disbands a channel for the provided peer, this has to be executed by the server.
        /// </summary>
        private static bool DisbandChannelForPeer(long peerId, ulong steamId, string channelName)
        {
            if (ZNet.m_isServer)
            {
                if (!ServerChannelManager.DoesChannelExist(channelName))
                {
                    SendToPeer(peerId, ChannelDisbandResponseType.ChannelNotFound, channelName);
                }
                else if(!ServerChannelManager.CanModerateChannel(steamId, channelName))
                {
                    SendToPeer(peerId, ChannelDisbandResponseType.NoPermission, channelName);
                }
                else
                {
                    return ServerChannelManager.DisbandChannel(peerId, steamId, channelName);
                }
            }
            return false;
        }
    }
}
