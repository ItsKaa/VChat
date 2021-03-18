using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using VChat.Data.Messages;
using VChat.Messages;

namespace VChat.Services
{
    public static class ServerChannelManager
    {
        private static List<ServerChannelInfo> ServerChannelInfo { get; set; }
        private static List<ServerChannelInviteInfo> ChannelInviteInfo { get; set; }
        private static readonly object _lockChannelInfo = new();
        private static readonly object _lockChannelInviteInfo = new();
        
        // TODO: Setting
        public const bool CanUsersCreateChannels = true;

        static ServerChannelManager()
        {
            ServerChannelInfo = new List<ServerChannelInfo>();
            ChannelInviteInfo = new List<ServerChannelInviteInfo>();
        }

        public static bool DoesChannelExist(string name)
        {
            lock(_lockChannelInfo)
            {
                foreach(var channel in ServerChannelInfo)
                {
                    if(name.Equals(channel.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a copy of the current channel list
        /// </summary>
        public static List<ServerChannelInfo> GetServerChannelInfoCopy()
        {
            lock(_lockChannelInfo)
            {
                return ServerChannelInfo.ToList();
            }
        }

        /// <summary>
        /// Returns the list of available channels for the user with steamId
        /// </summary>
        public static IEnumerable<ServerChannelInfo> GetChannelsForUser(ulong steamId)
        {
            lock (_lockChannelInfo)
            {
                return ServerChannelInfo.Where(x => x.IsPublic || x.OwnerId == steamId || x.Invitees.Contains(steamId));
            }
        }

        /// <summary>
        /// Returns a list of known invites for the provided user.
        /// </summary>
        public static IEnumerable<ServerChannelInviteInfo> GetChannelInvitesForUser(ulong steamId)
        {
            lock(_lockChannelInviteInfo)
            {
                return ChannelInviteInfo.Where(x => x.InviteeId == steamId);
            }
        }

        /// <summary>
        /// Adds a channel to the server.
        /// </summary>
        public static bool AddChannel(string name, ulong ownerSteamId, bool isPublic, bool isAdminConsideredAnOwner)
        {
            if(DoesChannelExist(name))
            {
                return false;
            }

            var channelInfo = new ServerChannelInfo()
            {
                Name = name,
                OwnerId = ownerSteamId,
                IsPublic = isPublic,
                ServerCommandName = name.ToLower().Replace(" ", ""),
            };

            lock (_lockChannelInfo)
            {
                ServerChannelInfo.Add(channelInfo);
            }

            // Send update message to relevant players
            foreach (var peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer.m_socket is ZSteamSocket steamSocket
                    && steamSocket.GetPeerID().m_SteamID == ownerSteamId)
                {
                    SendChannelInformationToClient(peer.m_uid);
                    SendMessageToClient_ChannelConnected(peer.m_uid, channelInfo);
                }
            }
            return false;
        }

        /// <summary>
        /// Add a player to the invitee list of an existing channel.
        /// </summary>
        public static bool AddPlayerToChannelInviteeList(string channelName, ulong inviteeId)
        {
            lock (_lockChannelInfo)
            {
                var channelInfo = ServerChannelInfo.FirstOrDefault(x => string.Equals(channelName, x.Name, StringComparison.CurrentCultureIgnoreCase));
                if (channelInfo != null)
                {
                    if (!channelInfo.Invitees.Contains(inviteeId))
                    {
                        channelInfo.Invitees.Add(inviteeId);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the user is an administrator on the server
        /// </summary>
        public static bool IsAdministrator(ulong steamId)
        {
            return ZNet.instance.m_adminList.Contains($"{steamId}");
        }

        /// <summary>
        /// Returns the peer with the provided steam id, or null.
        /// </summary>
        public static ZNetPeer FindPeerBySteamId(ulong steamId)
        {
            foreach (var peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer.m_socket is ZSteamSocket steamSocket
                    && steamSocket.GetPeerID().m_SteamID == steamId)
                {
                    return peer;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the peer from a peer id
        /// </summary>
        public static ZNetPeer GetPeer(long peerId)
        {
            return ZNet.instance.GetPeer(peerId);
        }

        /// <summary>
        /// Get the steam id from a peer.
        /// </summary>
        public static bool GetSteamIdFromPeer(ZNetPeer peer, out ulong steamId)
        {
            if (peer?.m_socket is ZSteamSocket steamSocket)
            {
                steamId = steamSocket.GetPeerID().m_SteamID;
                return true;
            }

            steamId = ulong.MaxValue;
            return false;
        }

        /// <summary>
        /// Get the steam id from a peer.
        /// </summary>
        public static bool GetSteamIdFromPeer(long peerId, out ulong steamId, out ZNetPeer peer)
        {
            peer = GetPeer(peerId);
            return GetSteamIdFromPeer(peer, out steamId);
        }

        /// <summary>
        /// Get the steam id from a peer.
        /// </summary>
        public static bool GetSteamIdFromPeer(long peerId, out ulong steamId)
            => GetSteamIdFromPeer(peerId, out steamId, out ZNetPeer _);

        /// <summary>
        /// Returns true if the channel can be found and if the user has permission to invite.
        /// </summary>
        public static bool CanInvite(ulong steamId, string channelName)
        {
            lock (_lockChannelInfo)
            {
                var channel = ServerChannelInfo.FirstOrDefault(x => string.Equals(x.Name, channelName, StringComparison.CurrentCultureIgnoreCase));
                return CanInvite(steamId, channel);
            }
        }

        /// <summary>
        /// Returns true if the channel can be found and if the user has permission to invite.
        /// </summary>
        public static bool CanInvite(ulong steamId, ServerChannelInfo channelInfo)
        {
            return channelInfo != null
                && channelInfo.OwnerId == steamId
                || channelInfo.Invitees.Contains(steamId)
                || IsAdministrator(steamId);
        }

        /// <summary>
        /// Sends the accessible channel information to the peer.
        /// </summary>
        public static bool SendChannelInformationToClient(long peerId)
        {
            if (ZNet.m_isServer)
            {
                var peer = ZNet.instance.GetPeer(peerId);
                var steamId = (peer.m_socket as ZSteamSocket)?.GetPeerID().m_SteamID;
                if (steamId != null)
                {
                    VChatPlugin.LogError($"Sending channel info to {steamId}");
                    var channels = GetChannelsForUser(steamId.Value);
                    ChannelInfoMessage.SendToPeer(peerId, channels);
                    return true;
                }

                VChatPlugin.LogError($"Steam ID is undefined for {peerId}");
            }

            return false;
        }

        /// <summary>
        /// Sends a request to the server to create a channel.
        /// </summary>
        public static bool ClientSendAddChannelToServer(long peerId, ulong steamId, string channelName)
        {
            if (DoesChannelExist(channelName))
            {
                ChannelCreateMessage.SendFailedResponseToPeer(peerId, ChannelCreateMessage.ChannelCreateResponseType.ChannelAlreadyExists, channelName);
            }
            else
            {
                if (!CanUsersCreateChannels && !IsAdministrator(steamId))
                {
                    ChannelCreateMessage.SendFailedResponseToPeer(peerId, ChannelCreateMessage.ChannelCreateResponseType.NoPermission, channelName);
                }
                else
                {
                    VChatPlugin.LogWarning($"Creating channel named {channelName}.");
                    return AddChannel(channelName, steamId, false, true);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends an invite to a peer, from a peer, if it has the permission to do so.
        /// </summary>
        public static bool InvitePlayerToChannel(string channelName, long inviterPeerId, ulong inviterSteamId, ulong inviteeId)
        {
            if (CanInvite(inviterSteamId, channelName))
            {
                var peer = FindPeerBySteamId(inviteeId);
                if(peer != null)
                {
                    var inviteInfo = new ServerChannelInviteInfo()
                    {
                        InviteeId = inviteeId,
                        InviterId = inviterSteamId,
                        ChannelName = channelName,
                    };
                    lock (_lockChannelInviteInfo)
                    {
                        ChannelInviteInfo.Add(inviteInfo);
                    }
                    SendInviteRequestToPeer(peer.m_uid, inviteInfo);
                }
                else
                {
                    ChannelInviteMessage.SendFailedResponseToPeer(inviterPeerId, ChannelInviteMessage.ChannelInviteResponseType.UserNotFound, channelName);
                    return false;
                }
                return true;
            }
            else
            {
                ChannelInviteMessage.SendFailedResponseToPeer(inviterPeerId, ChannelInviteMessage.ChannelInviteResponseType.NoPermission, channelName);
            }
            return false;
        }

        public static bool AcceptChannelInvite(long peerId, string channelName)
        {
            if (ZNet.m_isServer)
            {
                if (GetSteamIdFromPeer(peerId, out ulong steamId, out ZNetPeer peer))
                {
                    // Find the invite and remove it if present
                    var foundInvite = false;
                    lock (_lockChannelInviteInfo)
                    {
                        var inviteInfo = ChannelInviteInfo.FirstOrDefault(x =>
                               x.InviteeId == steamId
                            && string.Equals(x.ChannelName, channelName, StringComparison.CurrentCultureIgnoreCase)
                        );
                        if (inviteInfo != null)
                        {
                            ChannelInviteInfo.Remove(inviteInfo);
                            foundInvite = true;
                        }
                    }

                    // Handle the invite
                    if (foundInvite)
                    {
                        if (AddPlayerToChannelInviteeList(channelName, steamId))
                        {
                            ServerChannelInfo channelInfo;
                            lock (_lockChannelInfo)
                            {
                                channelInfo = ServerChannelInfo.FirstOrDefault(x => string.Equals(channelName, x.Name, StringComparison.CurrentCultureIgnoreCase));
                            }

                            if (channelInfo != null)
                            {
                                VChatPlugin.LogWarning($"User '{peer.m_playerName}' accepted channel invite '{channelName}' exist?");
                                SendChannelInformationToClient(peerId);
                                SendMessageToClient_ChannelConnected(peerId, channelInfo);
                                return true;
                            }
                        }
                        else
                        {
                            VChatPlugin.LogWarning($"Channel invite accepted but somehow the channel wasn't found, does the channel '{channelName}' exist?");
                        }

                    }
                    else
                    {
                        ChannelInviteMessage.SendFailedResponseToPeer(peerId, ChannelInviteMessage.ChannelInviteResponseType.NoInviteFound, channelName);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a message to the client that it's connected to a channel.
        /// </summary>
        public static void SendMessageToClient_ChannelConnected(long peerId, ServerChannelInfo channelInfo)
        {
            if (ZNet.m_isServer)
            {
                var peer = ZNet.instance.GetPeer(peerId);
                if (peer != null)
                {
                    var message = $"Successfully connected to the {channelInfo.Name} channel.";
                    object[] parameters = new object[] { peer.GetRefPos(), (int)Talker.Type.Normal, VChatPlugin.Name, message };
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ChatMessage", parameters);

                    if (GreetingMessage.PeerInfo.TryGetValue(peerId, out Data.GreetingMessagePeerInfo peerInfo)
                        && !peerInfo.HasReceivedGreeting)
                    {
                        // Only send if command name is set, otherwise it's considered a read-only channel.
                        if (!string.IsNullOrEmpty(channelInfo.ServerCommandName))
                        {
                            message = $"Type {VChatPlugin.Settings.CommandPrefix}{channelInfo.ServerCommandName} [text] to send a message in the {channelInfo.Name} chat.";
                            parameters = new object[] { peer.GetRefPos(), (int)Talker.Type.Normal, VChatPlugin.Name, message };
                            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ChatMessage", parameters);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sends an invite request to a client, this comes from another client but the server redirects it.
        /// </summary>
        public static void SendInviteRequestToPeer(long peerId, ServerChannelInviteInfo channelInviteInfo)
        {
            if (ZNet.m_isServer)
            {
                var peer = ZNet.instance.GetPeer(peerId);
                if (peer != null)
                {
                    VChatPlugin.LogWarning($"Sending channel invite for \"{channelInviteInfo.ChannelName}\" to \"{peer.m_playerName}\" ({peerId}).");

                    string message = $"{peer.m_playerName} wishes to invite you into the channel '{channelInviteInfo.ChannelName}'. Please type /accept to accept or /decine to decline.";
                    var parameters = new object[] { peer.GetRefPos(), (int)Talker.Type.Normal, VChatPlugin.Name, message };
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ChatMessage", parameters);
                }
            }
        }
    }
}
