using System.Linq;
using VChat.Data.Messages;
using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelInviteMessage
    {
        public enum ChannelInviteType
        {
            Invite,
            Accept,
            Decline,
        }

        public enum ChannelInviteResponseType
        {
            OK,
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
            else
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelInviteMessageHashName, OnMessage_Client);
            }
        }

        private static void OnMessage_Server(long senderId, ZPackage package)
        {
            var version = package.ReadInt();
            var senderPeerId = package.ReadLong();
            int requestType = package.ReadInt();

            // Only use the packet peer id if it's sent from the server.
            if (senderId != ValheimHelper.GetServerPeerId())
            {
                senderPeerId = senderId;
            }

            var senderPeer = ZNet.instance.GetPeer(senderPeerId);
            if (ValheimHelper.GetSteamIdFromPeer(senderPeer, out ulong senderSteamId))
            {
                if (requestType == (int)ChannelInviteType.Invite)
                {
                    var channelName = package.ReadString();
                    var targetPlayerName = package.ReadString();

                    var targetPeer = ZNet.instance.GetPeerByPlayerName(targetPlayerName);
                    if (targetPeer != null
                        && ValheimHelper.GetSteamIdFromPeer(targetPeer, out ulong targetSteamId))
                    {
                        VChatPlugin.Log($"Player \"{senderPeer.m_playerName}\" ({senderPeerId}) invited player \"{targetPeer.m_playerName}\" ({targetPeer.m_uid}) into {channelName}.");
                        InvitePlayerToChannel(channelName, senderPeerId, senderSteamId, targetSteamId);
                    }
                    else
                    {
                        VChatPlugin.LogWarning($"Unable to find the target player {targetPlayerName} to invite");
                    }
                }
                else if (requestType == (int)ChannelInviteType.Accept)
                {
                    var channelName = package.ReadString();
                    AcceptChannelInvite(senderPeerId, channelName);
                }
                else if (requestType == (int)ChannelInviteType.Decline)
                {
                    var channelName = package.ReadString();
                    DeclineChannelInvite(senderPeerId, channelName);
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Unable to retrieve steam id from peer {senderPeerId}.");
            }
        }

        private static void OnMessage_Client(long senderId, ZPackage package)
        {
            if (!ZNet.m_isServer)
            {
                if (senderId == ValheimHelper.GetServerPeerId())
                {
                    // Primarily a placeholder for later.
                    VChatPlugin.Log($"Channel invite message received from the server");
                }
                else
                {
                    VChatPlugin.LogWarning($"Ignoring a channel invite received from a client with id {senderId}.");
                }
            }
        }

        /// <summary>
        /// Send an invite message to a peer, this can only be executed by the server.
        /// </summary>
        public static void SendToPeer(long peerId, ChannelInviteType inviteType, ChannelInviteResponseType responseType, string channelName, string inviterName = null)
        {
            if (ZNet.m_isServer)
            {
                var package = new ZPackage();
                package.Write(Version);
                package.Write((int)inviteType);
                package.Write((int)responseType);
                package.Write(channelName ?? string.Empty);
                package.Write(inviterName ?? string.Empty);

                VChatPlugin.Log($"Sending response ({responseType}) to {peerId} for channel \"{channelName}\".");
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, ChannelInviteMessageHashName, package);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the invite message to a client.");
            }
        }

        /// <summary>
        /// Send an invite message to the server
        /// </summary>
        public static void SendToServer(long peerId, ChannelInviteType inviteType, string channelName, string targetPlayerName = null)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(peerId);
            package.Write((int)inviteType);
            package.Write(channelName ?? string.Empty);

            if (inviteType == ChannelInviteType.Invite)
            {
                package.Write(targetPlayerName ?? string.Empty);
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelInviteMessageHashName, package);
        }

        /// <summary>
        /// Sends an invite to a peer, from a peer, if it has the permission to do so.
        /// </summary>
        public static bool InvitePlayerToChannel(string channelName, long inviterPeerId, ulong inviterSteamId, ulong inviteeId)
        {
            if (ServerChannelManager.CanInvite(inviterSteamId, channelName))
            {
                var peer = ValheimHelper.FindPeerBySteamId(inviteeId);
                if (peer != null)
                {
                    var inviteInfo = new ServerChannelInviteInfo()
                    {
                        InviteeId = inviteeId,
                        InviterId = inviterSteamId,
                        ChannelName = channelName,
                    };

                    ServerChannelManager.AddInvite(peer, inviteInfo);
                    SendInviteRequestToPeer(peer, inviteInfo);
                }
                else
                {
                    SendToPeer(inviterPeerId, ChannelInviteType.Invite, ChannelInviteResponseType.UserNotFound, channelName);
                    return false;
                }
                return true;
            }
            else
            {
                SendToPeer(inviterPeerId, ChannelInviteType.Invite, ChannelInviteResponseType.NoPermission, channelName);
            }
            return false;
        }

        /// <summary>
        /// Sends an invite request to a client, this comes from another client but the server redirects it.
        /// </summary>
        public static void SendInviteRequestToPeer(ZNetPeer inviteePeer, ServerChannelInviteInfo channelInviteInfo)
        {
            // Send the invite message to the client
            if (ZNet.m_isServer)
            {
                var inviterPeer = ValheimHelper.GetPeerFromSteamId(channelInviteInfo.InviterId);
                if (inviteePeer != null && inviterPeer != null)
                {
                    VChatPlugin.Log($"Sending channel invite for \"{channelInviteInfo.ChannelName}\" to \"{inviteePeer.m_playerName}\" ({inviteePeer.m_uid}).");

                    string text = $"{inviterPeer.m_playerName} wishes to invite you into the channel '{channelInviteInfo.ChannelName}'. Please type /accept to accept or /decine to decline.";
                    MessageHelper.SendMessageToPeer(inviteePeer.m_uid, VChatPlugin.Name, null, text, () =>
                    {
                        // Send packet to the peer and a chat message in the VChat channel.
                        SendToPeer(inviteePeer.m_uid, ChannelInviteType.Invite, ChannelInviteResponseType.OK, channelInviteInfo.ChannelName, inviterPeer.m_playerName);
                        ServerChannelManager.SendMessageToPeerInChannel(inviteePeer.m_uid, channelInviteInfo.ChannelName, text);
                    });

                }
                else
                {
                    VChatPlugin.LogWarning($"Peer id could not be found for steam id {channelInviteInfo.InviteeId} or {channelInviteInfo.InviterId} from invite request");
                }
            }
        }

        /// <summary>
        /// Accept invite from a channel
        /// </summary>
        public static bool AcceptChannelInvite(long peerId, string channelName)
        {
            if (ZNet.m_isServer)
            {
                if (ValheimHelper.GetSteamIdFromPeer(peerId, out ulong steamId, out ZNetPeer peer))
                {
                    // Get the first invite if the channel name is empty.
                    if(string.IsNullOrEmpty(channelName))
                    {
                        channelName = ServerChannelManager.GetChannelInvitesForUser(steamId).FirstOrDefault()?.ChannelName ?? string.Empty;
                    }

                    if (ServerChannelManager.RemoveInvite(steamId, channelName))
                    {
                        if (ServerChannelManager.AddPlayerToChannelInviteeList(channelName, steamId))
                        {
                            ServerChannelInfo channelInfo = ServerChannelManager.FindChannel(channelName);
                            if (channelInfo != null)
                            {
                                VChatPlugin.Log($"User '{peer.m_playerName}' accepted channel invite '{channelName}' exist?");
                                ServerChannelManager.SendChannelInformationToClient(peerId);
                                ServerChannelManager.SendMessageToClient_ChannelConnected(peerId, channelInfo);
                                SendToPeer(peerId, ChannelInviteType.Accept, ChannelInviteResponseType.OK, channelInfo.Name);
                                return true;
                            }
                            else
                            {
                                SendToPeer(peerId, ChannelInviteType.Accept, ChannelInviteResponseType.NoInviteFound, channelInfo.Name);
                            }
                        }
                        else
                        {
                            VChatPlugin.LogWarning($"Channel invite accepted but somehow the channel wasn't found, does the channel '{channelName}' exist?");
                        }
                    }
                    else
                    {
                        SendToPeer(peerId, ChannelInviteType.Accept, ChannelInviteResponseType.NoInviteFound, channelName);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Declines a channel invite for a peer. Must be executed by the server.
        /// </summary>
        private static void DeclineChannelInvite(long peerId, string channelName)
        {
            if (ZNet.m_isServer)
            {
                if (ValheimHelper.GetSteamIdFromPeer(peerId, out ulong steamId, out ZNetPeer peer))
                {
                    if (string.IsNullOrEmpty(channelName))
                    {
                        var invites = ServerChannelManager.GetChannelInvitesForUser(steamId);
                        if (invites?.Count() > 0)
                        {
                            ServerChannelManager.DeclineChannelInvite(peer.m_uid, invites.FirstOrDefault().ChannelName);
                        }
                        else
                        {
                            SendToPeer(peer.m_uid, ChannelInviteType.Decline, ChannelInviteResponseType.NoInviteFound, channelName);
                        }
                    }
                    else
                    {
                        ServerChannelManager.DeclineChannelInvite(peer.m_uid, channelName);
                    }
                }
            }
        }

    }
}
