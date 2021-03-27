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
            UserAlreadyInChannel,
            ChannelNotFound,
            NoPermission,
            InvitedUserToChannel,
        }

        public enum ChannelInviteAcceptResponseType
        {
            OK,
            NoInviteFound,
        }

        public enum ChannelInviteDeclineResponseType
        {
            OK,
            InviteeDeclined,
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

                    var targetPeer = ValheimHelper.FindPeerByPlayerName(targetPlayerName);
                    if (targetPeer != null
                        && ValheimHelper.GetSteamIdFromPeer(targetPeer, out ulong targetSteamId))
                    {
                        VChatPlugin.Log($"Player \"{senderPeer.m_playerName}\" ({senderPeerId}) invited player \"{targetPeer.m_playerName}\" ({targetPeer.m_uid}) into {channelName}.");
                        InvitePlayerToChannel(channelName, senderPeerId, senderSteamId, targetSteamId);
                    }
                    else
                    {
                        SendToPeer(senderPeerId, ChannelInviteResponseType.UserNotFound, channelName, senderPeer.m_playerName);
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

        /// <summary>
        /// Send an invite message to a peer, this can only be executed by the server.
        /// </summary>
        public static void SendToPeer(long peerId, ChannelInviteResponseType responseType, string channelName, string inviterName = null, long? inviterPeerId = null)
        {
            if (ZNet.m_isServer)
            {
                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    bool isNotification = false;
                    string text = null;
                    switch (responseType)
                    {
                        case ChannelInviteResponseType.OK:
                            // Nothing for the target user.
                            break;
                        case ChannelInviteResponseType.InvitedUserToChannel:
                            text = $"Invited user {ValheimHelper.GetPeer(inviterPeerId ?? long.MaxValue)?.m_playerName ?? ""} to the channel {channelName}.";
                            isNotification = true;
                            break;
                        case ChannelInviteResponseType.ChannelNotFound:
                            text = "Cannot find a channel with that name.";
                            break;
                        case ChannelInviteResponseType.UserNotFound:
                            text = "Couldn't find a user with that name.";
                            break;
                        case ChannelInviteResponseType.UserAlreadyInChannel:
                            text = "User has already joined that channel.";
                            break;
                        case ChannelInviteResponseType.NoPermission:
                            text = "You do not have permission to invite players to that channel.";
                            break;
                        default:
                            VChatPlugin.LogError($"Unknown response type for channel invite received: {responseType}");
                            break;
                    }

                    VChatPlugin.Log($"[Channel Invite] Sending response {responseType} to peer {peerId} for channel {channelName} from inviter {inviterName}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (isNotification)
                        {
                            ServerChannelManager.SendMessageToPeerInChannel(peerId, VChatPlugin.Name, null, text);
                        }
                        else
                        {
                            ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                        }
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the invite message response to a client.");
            }
        }

        /// <summary>
        /// Send an invite accept response message to a peer, this can only be executed by the server.
        /// </summary>
        public static void SendToPeer(long peerId, ChannelInviteAcceptResponseType responseType, string channelName, string inviterName = null, long? inviterPeerId = null)
        {
            if (ZNet.m_isServer)
            {
                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    string text = null;
                    switch (responseType)
                    {
                        case ChannelInviteAcceptResponseType.OK:
                            // We do not need any response because ServerChannelManager will send a message to every online player in the channel when someone joins it.
                            break;

                        case ChannelInviteAcceptResponseType.NoInviteFound:
                            text = "You have not been invited to a channel.";
                            break;

                        default:
                            VChatPlugin.LogError($"Unknown response type for channel invite accept received: {responseType}");
                            break;
                    }

                    VChatPlugin.Log($"[Channel InviteAccept] Sending response {responseType} to peer {peerId} for channel {channelName} from inviter {inviterName}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the invite accept response message to a client.");
            }
        }

        /// <summary>
        /// Send an invite accept response message to a peer, this can only be executed by the server.
        /// </summary>
        public static void SendToPeer(long peerId, ChannelInviteDeclineResponseType responseType, string channelName, string inviterName = null, long? inviterPeerId = null)
        {
            if (ZNet.m_isServer)
            {
                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    bool isSuccess = false;
                    bool isNotification = false;
                    string text = null;
                    switch (responseType)
                    {
                        case ChannelInviteDeclineResponseType.OK:
                            {
                                text = $"Declined the invite for channel '{channelName}.'";
                                isSuccess = true;
                            }
                            break;
                        case ChannelInviteDeclineResponseType.InviteeDeclined:
                            {
                                text = $"Player {ValheimHelper.GetPeer(inviterPeerId ?? long.MaxValue)?.m_playerName} declined your invite to channel {channelName}.";
                                isNotification = true;
                            }
                            break;
                        case ChannelInviteDeclineResponseType.NoInviteFound:
                            text = "You have not been invited to a channel.";
                            break;

                        default:
                            VChatPlugin.LogError($"Unknown response type for channel invite accept received: {responseType}");
                            break;
                    }

                    VChatPlugin.Log($"[Channel InviteDecline] Sending response {responseType} to peer {peerId} for channel {channelName} from inviter {inviterName}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (isSuccess)
                        {
                            ServerChannelManager.SendVChatSuccessMessageToPeer(peerId, text);
                        }
                        else if (isNotification)
                        {
                            ServerChannelManager.SendMessageToPeerInChannel(peerId, VChatPlugin.Name, null, text);
                        }
                        else
                        {
                            ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                        }
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the invite decline response message to a client.");
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
            var peer = ValheimHelper.FindPeerBySteamId(inviteeId);
            var channel = ServerChannelManager.FindChannel(channelName);

            if (channel == null)
            {
                SendToPeer(inviterPeerId, ChannelInviteResponseType.ChannelNotFound, channelName);
            }
            else if (peer == null)
            {
                SendToPeer(inviterPeerId, ChannelInviteResponseType.UserNotFound, channel.Name);
            }
            else if (!ServerChannelManager.CanInvite(inviterSteamId, channelName))
            {
                SendToPeer(inviterPeerId, ChannelInviteResponseType.NoPermission, channel.Name);
            }
            else if (channel.GetSteamIds().Contains(inviteeId))
            {
                SendToPeer(inviterPeerId, ChannelInviteResponseType.UserAlreadyInChannel, channel.Name);
            }
            else
            {
                var inviteInfo = new ServerChannelInviteInfo()
                {
                    InviteeId = inviteeId,
                    InviterId = inviterSteamId,
                    ChannelName = channelName,
                };

                ServerChannelManager.AddInvite(inviteInfo);
                SendInviteRequestToPeer(peer, inviteInfo);
                return true;
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

                    string text = $"{inviterPeer.m_playerName} wishes to invite you to the channel '{channelInviteInfo.ChannelName}'. Please type /accept or /decline.";
                    ServerChannelManager.SendMessageToPeerInChannel(inviteePeer.m_uid, VChatPlugin.Name, null, text);

                    if (inviterPeer.m_uid != inviteePeer.m_uid)
                    {
                        SendToPeer(inviterPeer.m_uid, ChannelInviteResponseType.InvitedUserToChannel, channelInviteInfo.ChannelName, inviterPeer.m_playerName, inviteePeer.m_uid);
                    }
                }
                else
                {
                    VChatPlugin.LogError($"Peer id could not be found for steam id {channelInviteInfo.InviteeId} or {channelInviteInfo.InviterId} from invite request, this should have been handled.");
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
                        if (ServerChannelManager.AddPlayerToChannelInviteeList(peerId, steamId, channelName))
                        {
                            ServerChannelInfo channelInfo = ServerChannelManager.FindChannel(channelName);
                            if (channelInfo != null)
                            {
                                VChatPlugin.Log($"User '{peer.m_playerName}' accepted channel invite '{channelName}' exist?");
                                SendToPeer(peerId, ChannelInviteAcceptResponseType.OK, channelInfo.Name);
                                return true;
                            }
                            else
                            {
                                SendToPeer(peerId, ChannelInviteAcceptResponseType.NoInviteFound, channelInfo.Name);
                            }
                        }
                        else
                        {
                            VChatPlugin.LogWarning($"Channel invite accepted but somehow the channel wasn't found, did the channel '{channelName}' get deleted before the player accepted?");
                            SendToPeer(peerId, ChannelInviteAcceptResponseType.NoInviteFound, channelName);
                        }
                    }
                    else
                    {
                        SendToPeer(peerId, ChannelInviteAcceptResponseType.NoInviteFound, channelName);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Declines a channel invite for a peer. Must be executed by the server.
        /// </summary>
        public static void DeclineChannelInvite(long peerId, string channelName)
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
                            channelName = invites.FirstOrDefault()?.ChannelName;
                            if (ServerChannelManager.DeclineChannelInvite(peer.m_uid, channelName))
                            {
                                SendToPeer(peerId, ChannelInviteDeclineResponseType.OK, channelName);
                            }
                        }
                        else
                        {
                            SendToPeer(peer.m_uid, ChannelInviteDeclineResponseType.NoInviteFound, channelName);
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
