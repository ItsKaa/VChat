using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VChat.Data.Messages;
using VChat.Extensions;
using VChat.Helpers;
using VChat.Messages;

namespace VChat.Services
{
    public static class ServerChannelManager
    {
        private static List<ServerChannelInfo> ServerChannelInfo { get; set; }
        private static List<ServerChannelInviteInfo> ChannelInviteInfo { get; set; }
        private static readonly object _lockChannelInfo = new();
        private static readonly object _lockChannelInviteInfo = new();
        public const ulong ServerOwnerId = 0ul;

        // TODO: Setting
        public const bool CanUsersCreateChannels = true;

        static ServerChannelManager()
        {
            ServerChannelInfo = new List<ServerChannelInfo>();
            ChannelInviteInfo = new List<ServerChannelInviteInfo>();

            // Add VChat channel, this is used to send informational messages.
            AddChannelsDirect(new ServerChannelInfo()
            {
                Name = VChatPlugin.Name,
                IsPluginOwnedChannel = true,
                Color = new Color(0.035f, 0.714f, 0.902f),
                OwnerId = ServerOwnerId,
                IsPublic = true,
            });
        }

        public static bool DoesChannelExist(string name)
        {
            lock (_lockChannelInfo)
            {
                foreach (var channel in ServerChannelInfo)
                {
                    if (ValheimHelper.NameEquals(name, channel.Name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a copy of the current channel list. Do not modify anything here directly.
        /// </summary>
        public static IEnumerable<ServerChannelInfo> GetServerChannelInfoCollection()
        {
            lock (_lockChannelInfo)
            {
                return ServerChannelInfo.Select(x => new ServerChannelInfo(x)).ToList();
            }
        }

        /// <summary>
        /// Returns the list of available channels for the user with steamId. This returns a copy of the actual object.
        /// </summary>
        public static IEnumerable<ServerChannelInfo> GetChannelsForUser(ulong steamId)
        {
            return GetServerChannelInfoCollection().Where(x => x.IsPublic || x.OwnerId == steamId || x.Invitees.Contains(steamId));
        }

        /// <summary>
        /// Returns true if the user has special permissions for that channel.
        /// This means that the user can kick playerss and disband the channel.
        /// </summary>
        public static bool CanModerateChannel(ulong steamId, string channelName)
        {
            lock (_lockChannelInfo)
            {
                var channel = FindChannel(channelName);
                if(channel == null)
                {
                    return false;
                }
                else
                {
                    if (ValheimHelper.IsAdministrator(steamId))
                    {
                        return true;
                    }
                    else
                    {
                        return channel.OwnerId == steamId;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of known invites for the provided user. This returns a copy of the actual object.
        /// </summary>
        public static IEnumerable<ServerChannelInviteInfo> GetChannelInvitesForUser(ulong steamId)
        {
            lock (_lockChannelInviteInfo)
            {
                return ChannelInviteInfo.Where(x => x.InviteeId == steamId).Select(x => new ServerChannelInviteInfo(x));
            }
        }

        /// <summary>
        /// Adds a channel to the server.
        /// </summary>
        internal static void AddChannelsDirect(params ServerChannelInfo[] serverChannelInfos)
        {
            lock (_lockChannelInfo)
            {
                foreach (var channelInfo in serverChannelInfos)
                {
                    if (DoesChannelExist(channelInfo.Name))
                    {
                        VChatPlugin.LogError($"AddChannelDirect: Cannot add channel named {channelInfo.Name} because it already exists");
                    }
                    else
                    {
                        ServerChannelInfo.Add(channelInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a channel to the server.
        /// </summary>
        public static bool AddChannel(string name, ulong ownerSteamId, bool isPublic, Color? color = null)
        {
            if (DoesChannelExist(name))
            {
                VChatPlugin.LogWarning($"{ownerSteamId} requested to create a channel named {name} but it already exists.");
                return false;
            }

            var channelInfo = new ServerChannelInfo()
            {
                Name = name,
                OwnerId = ownerSteamId,
                IsPublic = isPublic,
                Color = color ?? Color.white,
            };

            VChatPlugin.LogWarning($"Adding channel named {name} to the collection");
            lock (_lockChannelInfo)
            {
                ServerChannelInfo.Add(channelInfo);
            }

            // Re-init the server commands so that this one is added to it, might be a better way of doing it but so far valheim doesn't do much multithreading anyway.
            VChatPlugin.InitialiseServerCommands();

            // Send update message to relevant players
            foreach (var peer in ZNet.instance.GetConnectedPeers())
            {
                if (ValheimHelper.GetSteamIdFromPeer(peer, out ulong steamId)
                    && steamId == ownerSteamId)
                {
                    SendChannelInformationToClient(peer.m_uid);
                    SendMessageToClient_ChannelConnected(peer.m_uid, channelInfo);
                }
            }

            // Save data when channel is modified.
            PluginDataManager.Save();
            return true;
        }

        /// <summary>
        /// Find a channel by it's name, this will return the referenced object.
        /// </summary>
        internal static ServerChannelInfo FindChannel(string channelName)
        {
            lock (_lockChannelInfo)
            {
                return ServerChannelInfo.FirstOrDefault(x => ValheimHelper.NameEquals(channelName, x.Name));
            }
        }

        /// <summary>
        /// Add an invite to the collection and sends the request to the peer.
        /// This does not perform validation and is intended to be used as the server.
        /// </summary>
        /// <remarks>
        /// Also see method <see cref="ChannelInviteMessage.SendInviteRequestToPeer(ZNetPeer, ServerChannelInviteInfo)"/>
        /// </remarks>
        public static void AddInvite(ServerChannelInviteInfo inviteInfo)
        {
            VChatPlugin.Log($"Added invite, channelName: {inviteInfo.ChannelName}, invitee: {inviteInfo.InviteeId}, inviter: {inviteInfo.InviterId}");
            lock (_lockChannelInviteInfo)
            {
                ChannelInviteInfo.Add(new ServerChannelInviteInfo(inviteInfo));
            }
        }

        /// <summary>
        /// Remove a channel invite from a user. This does not perform any validation and doesn't send a reply to the invitee.
        /// </summary>
        public static bool RemoveInvite(ulong steamId, string channelName)
        {
            lock (_lockChannelInviteInfo)
            {
                var inviteInfo = ChannelInviteInfo.FirstOrDefault(x =>
                       x.InviteeId == steamId
                    && ValheimHelper.NameEquals(x.ChannelName, channelName)
                );

                if (inviteInfo != null)
                {
                    ChannelInviteInfo.Remove(inviteInfo);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add a player to the invitee list of an existing channel.
        /// </summary>
        public static bool AddPlayerToChannelInviteeList(long peerId, ulong steamId, string channelName)
        {
            lock (_lockChannelInfo)
            {
                var channelInfo = FindChannel(channelName);
                if (channelInfo != null && !channelInfo.IsPluginOwnedChannel)
                {
                    if (!channelInfo.Invitees.Contains(steamId))
                    {
                        channelInfo.Invitees.Add(steamId);

                        SendChannelInformationToClient(peerId);
                        SendMessageToClient_ChannelConnected(peerId, channelInfo);

                        var playerName = ValheimHelper.GetPeer(peerId)?.m_playerName;
                        if (!string.IsNullOrEmpty(playerName))
                        {
                            SendMessageToAllPeersInChannel(channelName, null, $"<i>{playerName} has joined the channel.</i>");
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove a player from an existing channel, if the user is the owner, the ownership will be passed on, or the channel will disband if it's empty.
        /// </summary>
        /// <param name="senderPeerId">The peer id of the player remvoing the target player, set this to 0 if sending from the server.</param>
        public static bool RemovePlayerFromChannel(long senderPeerId, long targetPeerId, ulong targetSteamId, string channelName)
        {
            bool result = false;
            bool hasOwnerChanged = false;

            var senderPeer = ValheimHelper.GetPeer(senderPeerId);
            ValheimHelper.GetSteamIdFromPeer(senderPeer, out ulong senderSteamId);

            lock (_lockChannelInfo)
            {
                var channelInfo = FindChannel(channelName);
                if (channelInfo != null)
                {
                    // If the owner is being removed, change the owner of the channel or disband it if it's empty.
                    if (channelInfo.OwnerId == targetSteamId)
                    {
                        if (channelInfo.Invitees.Count == 0)
                        {
                            var steamIdsForChannel = channelInfo.GetSteamIds();

                            // Channel is empty, disband it.
                            if (DisbandChannel(senderPeerId, senderSteamId, channelName))
                            {
                                // Also send message to the sender if it's not in the channel, this can be true if it's an administrator.
                                if (senderPeer != null && !steamIdsForChannel.Contains(senderSteamId))
                                {
                                    SendVChatSuccessMessageToPeer(senderPeerId, $"{channelInfo.Name} has been disbanded.");
                                }
                            }
                            return true;
                        }
                        else
                        {
                            // Change the owner.
                            // Retrieve the list of active steam ids
                            var steamIds = ZNet.instance.GetConnectedPeers().Select(peer =>
                            {
                                if (ValheimHelper.GetSteamIdFromPeer(peer, out ulong peerSteamId))
                                {
                                    return peerSteamId;
                                }
                                return ulong.MaxValue;
                            }).Where(x => x != ulong.MaxValue).Distinct();

                            // Get the first active player in that collection
                            ulong newOwnerId = 0UL;
                            foreach(var inviteeId in channelInfo.Invitees)
                            {
                                if(steamIds.Contains(inviteeId))
                                {
                                    newOwnerId = inviteeId;
                                    break;
                                }
                            }

                            // If we cannot find an online player, simply pass it on to the first invitee.
                            if(newOwnerId == 0UL)
                            {
                                newOwnerId = channelInfo.Invitees.FirstOrDefault();
                                VChatPlugin.Log($"Passing the channel '{channelInfo.Name}' to the first invitee because nobody else is online.");
                            }

                            // Apply the owner
                            channelInfo.OwnerId = newOwnerId;
                            channelInfo.Invitees.Remove(newOwnerId);
                            VChatPlugin.Log($"Owner has been removed from the channel, passing the channel '{channelInfo.Name}' to {newOwnerId}.");

                            hasOwnerChanged = true;
                            result = true;
                        }
                    }

                    result |= channelInfo.GetSteamIds().Contains(targetSteamId);
                    if (result)
                    {
                        var targetPlayerName = ValheimHelper.GetPeer(targetPeerId)?.m_playerName ?? $"{targetSteamId}";
                        var senderPlayerName = senderPeerId == 0L ? "the server" : senderPeer?.m_playerName ?? "unknown";

                        VChatPlugin.Log($"Player '{targetPlayerName}' has been removed from the channel '{channelName}' by {senderPlayerName}.");

                        // Notify all active players that the target has been removed from the channel.
                        if (senderPeerId == targetPeerId)
                        {
                            // The sender already receives a confirmation message, so first remove the player from the invitee list to avoid duplicates.
                            channelInfo.Invitees.Remove(targetSteamId);
                            SendMessageToAllPeersInChannel(channelName, null, $"<i>{targetPlayerName} has left the channel.</i>");
                        }
                        else
                        {
                            SendMessageToAllPeersInChannel(channelName, null, $"<i>{targetPlayerName} has been removed from the channel by {senderPlayerName}.</i>");

                            var channelSteamIds = channelInfo.GetSteamIds();

                            // Also send message to the previous owner if it's not part of the the channel anymore.
                            if (!channelSteamIds.Contains(targetSteamId))
                            {
                                SendMessageToPeerInChannel(targetPeerId, VChatPlugin.Name, null, $"<i>You have been removed from the channel {channelName} by {senderPlayerName}.</i>");
                            }

                            // Also send message to the sender if it's not in the channel, this can be true if it's an administrator.
                            if (senderPeer != null && !channelSteamIds.Contains(senderSteamId))
                            {
                                SendVChatSuccessMessageToPeer(senderPeerId, $"{targetPlayerName} has been removed from the channel.");
                            }

                        }

                        // Remove the invitee after the message is sent and update the channel list of that player.
                        channelInfo.Invitees.Remove(targetSteamId);
                        SendChannelInformationToClient(targetPeerId);
                    }

                    if(hasOwnerChanged)
                    {
                        var ownerPeer = ValheimHelper.GetPeerFromSteamId(channelInfo.OwnerId);
                        var ownerPlayerName = ownerPeer?.m_playerName ?? $"{channelInfo.OwnerId}";

                        SendMessageToAllPeersInChannel(channelName, null, $"<i>{ownerPlayerName} is the new owner of this channel.</i>");
                    }

                    // Save data when channel is modified.
                    if (result || hasOwnerChanged)
                    {
                        PluginDataManager.Save();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if the channel can be found and if the user has permission to invite.
        /// </summary>
        public static bool CanInvite(ulong steamId, string channelName)
        {
            lock (_lockChannelInfo)
            {
                var channel = FindChannel(channelName);
                return CanInvite(steamId, channel);
            }
        }

        /// <summary>
        /// Returns true if the channel can be found and if the user has permission to invite.
        /// </summary>
        public static bool CanInvite(ulong steamId, ServerChannelInfo channelInfo)
        {
            return channelInfo != null
                && (channelInfo.OwnerId == steamId
                && !channelInfo.IsPluginOwnedChannel
                || channelInfo.Invitees.Contains(steamId)
                || ValheimHelper.IsAdministrator(steamId));
        }

        /// <summary>
        /// Sends the accessible channel information to the peer.
        /// </summary>
        public static bool SendChannelInformationToClient(long peerId)
        {
            if (ZNet.m_isServer)
            {
                if (ValheimHelper.GetSteamIdFromPeer(peerId, out ulong steamId))
                {
                    var channels = GetChannelsForUser(steamId);
                    ChannelInfoMessage.SendToPeer(peerId, channels);
                    return true;
                }

                VChatPlugin.LogError($"Steam ID is undefined for {peerId}");
            }

            return false;
        }

        /// <summary>
        /// Sends the accessible channel information to the peer.
        /// </summary>
        public static bool SendChannelInformationToConnectedClients(string channelName)
        {
            if (ZNet.m_isServer)
            {
                var channel = FindChannel(channelName);
                if (channel != null)
                {
                    foreach (var steamId in channel.GetSteamIds())
                    {
                        var peer = ValheimHelper.GetPeerFromSteamId(steamId);
                        if(peer != null)
                        {
                            var channels = GetChannelsForUser(steamId);
                            ChannelInfoMessage.SendToPeer(peer.m_uid, channels);
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Disband a channel, this will check if the user has permission to disband and it will send the response back to the user.
        /// This will also send a message to every connected user if the channel did disband.
        /// </summary>
        public static bool DisbandChannel(long senderPeerId, ulong senderSteamId, string channelName)
        {
            if(!DoesChannelExist(channelName))
            {
                ChannelDisbandMessage.SendToPeer(senderPeerId, ChannelDisbandMessage.ChannelDisbandResponseType.ChannelNotFound, channelName);
            }
            else if (!CanModerateChannel(senderSteamId, channelName))
            {
                ChannelDisbandMessage.SendToPeer(senderPeerId, ChannelDisbandMessage.ChannelDisbandResponseType.NoPermission, channelName);
            }
            else
            {
                VChatPlugin.LogWarning($"Disbanding channel named {channelName}.");

                ServerChannelInfo channelInfo;
                lock(_lockChannelInfo)
                {
                    channelInfo = FindChannel(channelName);
                    if (channelInfo != null)
                    {
                        ServerChannelInfo.Remove(channelInfo);
                    }
                    else
                    {
                        ChannelDisbandMessage.SendToPeer(senderPeerId, ChannelDisbandMessage.ChannelDisbandResponseType.ChannelNotFound, channelName);
                        return false;
                    }
                }

                // Re-init the server commands so that this one is added to it, might be a better way of doing it but so far valheim doesn't do much multithreading anyway.
                VChatPlugin.InitialiseServerCommands();

                var senderPeer = ValheimHelper.GetPeer(senderPeerId);

                // Send a message to all connected users and the sender, in case this is an administrator it may mean it's not connected to the channel.
                foreach (var targetSteamId in channelInfo.GetSteamIds().Concat(new[] { senderSteamId }).Distinct())
                {
                    if(ValheimHelper.GetPeerIdFromSteamId(targetSteamId, out long targetPeerId))
                    {
                        // Notify all connected peers with the player that disbanded it.
                        var text = $"Channel '{channelInfo.Name}' has been disbanded by {senderPeer?.m_playerName ?? "Server"}.";

                        SendMessageToPeerInChannel(targetPeerId, VChatPlugin.Name, null, text);

                        // Update channel information for VChat clients.
                        SendChannelInformationToClient(targetPeerId);
                    }
                }

                // Save data when channel is modified.
                PluginDataManager.Save();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update channel color.
        /// </summary>
        public static bool EditChannelColor(string channelName, Color color, long senderPeerId = 0L)
        {
            ServerChannelInfo channel = null;
            lock (_lockChannelInfo)
            {
                channel = FindChannel(channelName);
                if (channel == null)
                {
                    return false;
                }
                else
                {
                    // Apply minimum alpha value
                    if (color.a < 0.20)
                    {
                        color = new Color(color.r, color.g, color.b, Math.Max(0.2f, color.a));
                    }

                    channel.Color = color;
                }
            }

            var senderPlayerName = senderPeerId == 0L ? "server" : ValheimHelper.GetPeer(senderPeerId)?.m_playerName;
            VChatPlugin.Log($"Player '{senderPlayerName}' has changed channel color of the channel '{channelName}' to '{color.ToHtmlString()}'.");

            SendChannelInformationToConnectedClients(channelName);
            SendMessageToAllPeersInChannel(channelName, null, "<i>Channel color has been modified.</i>");

            // Also send message to the sender if it's not in the channel, this can be true if it's an administrator.
            if (ValheimHelper.GetSteamIdFromPeer(senderPeerId, out ulong senderSteamId)
                && !channel.GetSteamIds().Contains(senderSteamId))
            {
                SendMessageToPeerInChannel(senderPeerId, VChatPlugin.Name, null, $"Modified the color for channel <color={channel.Color.ToHtmlString()}>{channel.Name}</color>.");
            }

            return true;
        }


        /// <summary>
        /// Send a message to all connected peers within the provided channel.
        /// </summary>
        public static bool SendMessageToAllUsersInChannel(long senderPeerId, string channelName, string callerName, string text, Color? customColor = null)
        {
            var senderPeer = ValheimHelper.GetPeer(senderPeerId);
            if (senderPeer != null && ValheimHelper.GetSteamIdFromPeer(senderPeer, out ulong senderSteamId))
            {
                foreach (var channel in GetChannelsForUser(senderSteamId))
                {
                    if (ValheimHelper.NameEquals(channel.Name, channelName))
                    {
                        foreach (var channelUserSteamId in channel.GetSteamIds())
                        {
                            var targetPeer = ValheimHelper.GetPeerFromSteamId(channelUserSteamId);
                            if (targetPeer != null)
                            {
                                ChannelChatMessage.SendToPeer(targetPeer.m_uid, channel.Name, senderPeer?.m_refPos ?? new Vector3(), senderPeer?.m_playerName ?? callerName, text, customColor);
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Send a message to a peer in the provided channel, note that this will not send it to any other user within that channel.
        /// </summary>
        public static bool SendMessageToPeerInChannel(long targetPeerId, string channelName, string callerName, string text, Color? color = null)
        {
            var peer = ValheimHelper.GetPeer(targetPeerId);
            if (peer != null && ValheimHelper.GetSteamIdFromPeer(peer, out ulong steamId))
            {
                var knownChannels = GetChannelsForUser(steamId);
                foreach (var channel in knownChannels)
                {
                    if (ValheimHelper.NameEquals(channel.Name, channelName))
                    {
                        ChannelChatMessage.SendToPeer(peer.m_uid, channel.Name, peer.m_refPos, callerName, text, color);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Send a message to the provided channel to all active players that have access to it.
        /// </summary>
        public static bool SendMessageToAllPeersInChannel(string channelName, string callerName, string text, Color? color = null)
        {
            var channelInfo = FindChannel(channelName);
            if (channelInfo != null)
            {
                // List of players ids to send that message to.
                foreach (var inviteeSteamId in channelInfo.GetSteamIds())
                {
                    if (ValheimHelper.GetPeerIdFromSteamId(inviteeSteamId, out long inviteePeerId))
                    {
                        SendMessageToPeerInChannel(inviteePeerId, channelName, callerName, text, color);
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sends an success message in the VChat channel to a user, colors everything green.
        /// </summary>
        /// <returns></returns>
        public static bool SendVChatSuccessMessageToPeer(long targetPeerId, string text)
        {
            return SendMessageToPeerInChannel(targetPeerId, VChatPlugin.Name, null, text, new Color(0.137f, 1f, 0f));
        }

        /// <summary>
        /// Sends an error message in the VChat channel to a user, colors everything red.
        /// </summary>
        /// <returns></returns>
        public static bool SendVChatErrorMessageToPeer(long targetPeerId, string text)
        {
            return SendMessageToPeerInChannel(targetPeerId, VChatPlugin.Name, null, text, Color.red);
        }

        public static bool DeclineChannelInvite(long peerId, string channelName)
        {
            if (ZNet.m_isServer)
            {
                if (ValheimHelper.GetSteamIdFromPeer(peerId, out ulong steamId, out ZNetPeer peer))
                {
                    // Find the invite and remove it if present
                    ServerChannelInviteInfo inviteInfo = null;
                    lock (_lockChannelInviteInfo)
                    {
                        inviteInfo = ChannelInviteInfo.FirstOrDefault(x =>
                               x.InviteeId == steamId
                            && ValheimHelper.NameEquals(x.ChannelName, channelName)
                        );

                        if (inviteInfo != null)
                        {
                            ChannelInviteInfo.Remove(inviteInfo);
                        }
                    }

                    // Handle the invite
                    if (inviteInfo != null)
                    {
                        VChatPlugin.LogWarning($"User '{peer.m_playerName}' declined channel invite '{channelName}'.");

                        // Also send a message to the inviter
                        if (ValheimHelper.GetPeerIdFromSteamId(inviteInfo.InviterId, out long inviterPeerId)
                            && inviterPeerId != peerId)
                        {
                            ChannelInviteMessage.SendToPeer(inviterPeerId, ChannelInviteMessage.ChannelInviteDeclineResponseType.InviteeDeclined, channelName, null, peerId);
                        }

                        return true;
                    }
                    else
                    {
                        ChannelInviteMessage.SendToPeer(peerId, ChannelInviteMessage.ChannelInviteDeclineResponseType.NoInviteFound, channelName);
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
            if (ZNet.m_isServer && !channelInfo.IsPluginOwnedChannel)
            {
                var peer = ZNet.instance?.GetPeer(peerId);
                if (peer != null)
                {
                    VChatPlugin.Log($"User {peer?.m_playerName} ({peerId}) has connected to the channel {channelInfo.Name}");

                    if (channelInfo.IsPluginOwnedChannel)
                    {
                        var message = $"Successfully connected to the {channelInfo.Name} channel.";
                        SendMessageToPeerInChannel(peerId, VChatPlugin.Name, null, message);
                    }
                    else
                    {
                        var message = $"Successfully connected to the channel {channelInfo.Name}. Type {VChatPlugin.Settings.CommandPrefix}{channelInfo.ServerCommandName} [text] to send a message to this channel.";
                        SendMessageToPeerInChannel(peerId, VChatPlugin.Name, null, message);
                    }
                }
            }
        }
    }
}
