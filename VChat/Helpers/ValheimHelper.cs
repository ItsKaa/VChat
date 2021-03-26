using System.Linq;
using VChat.Extensions;

namespace VChat.Helpers
{
    public static class ValheimHelper
    {
        /// <summary>
        /// Returns the server peer id.
        /// </summary>
        public static long GetServerPeerId()
        {
            return ZRoutedRpc.instance.GetServerPeerID();
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
        /// Returns the peer when the player name matches the input, or null if it cannot be found.
        /// Uses method NameEquals to compare.
        /// </summary>
        public static ZNetPeer FindPeerByPlayerName(string playerName)
        {
            foreach (var peer in ZNet.instance.GetConnectedPeers())
            {
                if(NameEquals(peer?.m_playerName, playerName))
                {
                    return peer;
                }
            }
            return null;
        }

        /// <summary>
        /// Compares two names, removes all formatting and unnecessary characters to perform a comparison.
        /// </summary>
        public static bool NameEquals(string name, string otherName)
        {
            // Remove unity formatting and whitespaces
            var strippedName = name?.StripRichTextFormatting()?.StripWhitespaces() ?? string.Empty;
            var strippedOtherName = otherName?.StripRichTextFormatting()?.StripWhitespaces() ?? string.Empty;

            // Compare
            return string.Equals(strippedName, strippedOtherName, System.StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Get the peer from a peer id
        /// </summary>
        public static ZNetPeer GetPeer(long peerId)
        {
            return ZNet.instance?.GetPeer(peerId);
        }

        /// <summary>
        /// Returns the local player's peer id.
        /// </summary>
        public static long GetLocalPlayerPeerId()
        {
            return ZNet.instance?.GetUID() ?? long.MaxValue;
        }

        /// <summary>
        /// Returns the local player's steam id. Not suited for servers.
        /// </summary>
        public static ulong GetLocalPlayerSteamId()
        {
            var hostname = ZNet.instance?.GetPlayerList()?.FirstOrDefault(x => x.m_characterID == ZNet.instance.m_characterID).m_host;
            if(ulong.TryParse(hostname, out ulong steamId) && steamId > 0)
            {
                return steamId;
            }

            return ulong.MaxValue;
        }

        /// <summary>
        /// Get the peer from a steam id, or null.
        /// </summary>
        public static ZNetPeer GetPeerFromSteamId(ulong steamId)
        {
            var peers = ZNet.instance.GetConnectedPeers();
            foreach (var peer in peers)
            {
                if (GetSteamIdFromPeer(peer, out ulong peerSteamId))
                {
                    if (peerSteamId == steamId)
                    {
                        return peer;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get the peer id from a steam id
        /// </summary>
        public static bool GetPeerIdFromSteamId(ulong steamId, out long peerId)
        {
            var peers = ZNet.instance.GetConnectedPeers();
            foreach (var peer in peers)
            {
                if (GetSteamIdFromPeer(peer, out ulong peerSteamId))
                {
                    if (peerSteamId == steamId)
                    {
                        peerId = peer.m_uid;
                        return true;
                    }
                }
            }

            peerId = long.MaxValue;
            return false;
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
    }
}
