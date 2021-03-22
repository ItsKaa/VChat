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
                ZRoutedRpc.instance.Register<ZPackage>(ChannelDisbandMessageHashName, OnMessage_Server);
            }
            else
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelDisbandMessageHashName, OnMessage_Client);
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

        private static void OnMessage_Client(long senderId, ZPackage package)
        {
            if (!ZNet.m_isServer)
            {
                if (senderId == ValheimHelper.GetServerPeerId())
                {
                    VChatPlugin.LogWarning($"Channel disband response received from server");
                }
                else
                {
                    VChatPlugin.LogWarning($"Ignoring a channel disband message received from a client with id {senderId}.");
                }
            }
        }

        public static void SendResponseToPeer(long peerId, ChannelDisbandResponseType responseType, string channelName)
        {
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write((int)responseType);
                package.Write(channelName);

                VChatPlugin.LogWarning($"Sending channel disband response ({responseType}) to {peerId} for channel \"{channelName}\".");
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ChannelDisbandMessageHashName, package);
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
                    SendResponseToPeer(peerId, ChannelDisbandResponseType.ChannelNotFound, channelName);
                }
                else
                {
                    if(!ServerChannelManager.CanDisbandChannel(steamId, channelName))
                    {
                        SendResponseToPeer(peerId, ChannelDisbandResponseType.NoPermission, channelName);
                    }
                    else
                    {
                        ServerChannelManager.DisbandChannel(peerId, steamId, channelName);
                    }
                }
            }
            return false;
        }
    }
}
