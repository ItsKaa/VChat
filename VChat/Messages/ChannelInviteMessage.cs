using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelInviteMessage
    {
        public enum ChannelInviteType
        {
            Invite,
            Accept,
        }

        public enum ChannelInviteResponseType
        {
            UserNotFound,
            NoPermission,
            NoInviteFound,
        }

        public const string ChannelInviteMessageHashName = VChatPlugin.Name + ".ChannelInvite";
        public const int Version = 1;

        static ChannelInviteMessage()
        {
        }

        public static void Register()
        {
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelInviteMessageHashName, OnMessage_Server);
            }
        }

        private static void OnMessage_Server(long senderId, ZPackage package)
        {
            if (senderId != ZNet.instance.GetServerPeer()?.m_uid)
            {
                var peer = ZNet.instance.GetPeer(senderId);
                if (peer != null && peer.m_socket is ZSteamSocket steamSocket)
                {
                    var version = package.ReadInt();
                    int requestType = package.ReadInt();
                    if (requestType == (int)ChannelInviteType.Invite)
                    {
                        ulong steamIdInvitee = package.ReadULong();
                        var channelName = package.ReadString();

                        // TODO: Refactor this
                        foreach (var connectedPeer in ZNet.instance.GetConnectedPeers())
                        {
                            if (connectedPeer.m_socket is ZSteamSocket connectedPeerSteamSocket)
                            {
                                var connectedPeerSteamId = connectedPeerSteamSocket.GetPeerID().m_SteamID;
                                if (connectedPeerSteamId == steamIdInvitee)
                                {
                                    VChatPlugin.LogWarning($"Player \"{peer.m_playerName}\" ({senderId}) invited player \"{connectedPeer.m_playerName}\" ({connectedPeer.m_uid}) into {channelName}.");
                                    ServerChannelManager.InvitePlayerToChannel(channelName, senderId, steamIdInvitee, connectedPeerSteamId);
                                    break;
                                }
                            }
                        }
                    }
                    else if(requestType == (int)ChannelInviteType.Accept)
                    {
                        //ulong steamIdInvitee = package.ReadULong();
                        var channelName = package.ReadString();
                        ServerChannelManager.AcceptChannelInvite(senderId, channelName);
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Ignoring a channel configuration message received from a server with id {senderId}.");
            }
        }

        public static void SendFailedResponseToPeer(long peerId, ChannelInviteResponseType responseType, string channelName)
        {
            // TODO: Handle if user doesn't have VChat
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write((int)responseType);
                package.Write(channelName);

                VChatPlugin.LogWarning($"Sending response ({responseType}) to {peerId} for channel \"{channelName}\".");
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ChannelInviteMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the greeing to a client.");
            }
        }

        public static void SendInviteRequestToServer(string channelName)
        {
            if (!ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write((int)ChannelInviteType.Invite);
                package.Write(channelName);

                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), ChannelInviteMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the greeing to a client.");
            }
        }

        public static void SendInviteRequestToClient(long peerId, string inviter, string channelName)
        {
            if (!ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write(channelName);
                package.Write(inviter);

                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), ChannelInviteMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the greeing to a client.");
            }
        }
    }
}
