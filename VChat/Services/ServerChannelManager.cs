using System;
using System.Collections.Generic;
using System.Linq;
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
                IsPublic = isPublic
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
        /// Determines if the user is an administrator on the server
        /// </summary>
        public static bool IsAdministrator(ulong steamId)
        {
            return ZNet.instance.m_adminList.Contains($"{steamId}");
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
    }
}
