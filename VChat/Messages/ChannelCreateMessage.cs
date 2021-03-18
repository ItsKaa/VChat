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
                ZRoutedRpc.instance.Register<ZPackage>(ChannelCreateMessageHashName, OnMessage_Client);
            }
        }

        private static void OnMessage_Client(long senderId, ZPackage package)
        {
            if (senderId != ZNet.instance.GetServerPeer()?.m_uid)
            {
                var peer = ZNet.instance.GetPeer(senderId);
                if (peer != null && peer.m_socket is ZSteamSocket steamSocket)
                {
                    var version = package.ReadInt();
                    var channelName = package.ReadString();

                    VChatPlugin.LogWarning($"Player \"{peer.m_playerName}\" ({senderId}) requested to create channel named {channelName}.");
                    ServerChannelManager.ClientSendAddChannelToServer(senderId, steamSocket.GetPeerID().m_SteamID, channelName);
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Ignoring a channel configuration message received from a client with id {senderId}.");
            }
        }

        public static void SendFailedResponseToPeer(long peerId, ChannelCreateResponseType responseType, string channelName)
        {
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write((int)responseType);
                package.Write(channelName);

                VChatPlugin.LogWarning($"Sending response ({responseType}) to {peerId} for channel \"{channelName}\".");
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ChannelCreateMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel create response to a client.");
            }
        }

        public static void SendRequestToServer(string channelName)
        {
            if (!ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write(channelName);

                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), ChannelCreateMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel create request to a client.");
            }
        }
    }
}
