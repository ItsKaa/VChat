using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using VChat.Data;
using VChat.Helpers;
using VChat.Messages;
using VChat.Services;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_CharacterID))]
    public static class ZNetPatchRPC_CharacterID
    {
        public static List<long> SpawnedPlayers { get; private set; }
        static ZNetPatchRPC_CharacterID()
        {
            SpawnedPlayers = new List<long>();
        }

        public static void Prefix(ref ZNet __instance, ref ZRpc rpc, ref ZDOID characterID)
        {
            // Client sends the character id to the server before it spawns.
            if (ZNet.m_isServer)
            {
                ZNetPeer peer = __instance.GetPeer(rpc);
                if (peer != null)
                {
                    if (!SpawnedPlayers.Contains(peer.m_uid))
                    {
                        bool isPlayerConnectedWithVChat = false;
                        if (GreetingMessage.PeerInfo.TryGetValue(peer.m_uid, out GreetingMessagePeerInfo peerInfo))
                        {
                            isPlayerConnectedWithVChat = peerInfo.HasReceivedGreeting;
                        }

                        // Send a local chat message to the player if it does not have VChat installed.
                        if (!isPlayerConnectedWithVChat)
                        {
                            VChatPlugin.Log($"Player \"{peer.m_playerName}\" ({peer.m_uid}) spawned that does not have VChat installed, sending mod information...");

                            var messages = new[]
                            {
                                $"This server runs {VChatPlugin.Name} {VChatPlugin.Version}{(VChatPlugin.IsBetaVersion ? " BETA" : "")}, We detected that you do not have this mod installed.",
                                $"You can find the latest version on {VChatPlugin.RepositoryUrl}",
                                $"{VChatPlugin.Name} channel messages will be sent in your local chat channel.",
                                $"Type {VChatPlugin.Settings.CommandPrefix}{VChatPlugin.Settings.GlobalChatCommandName.FirstOrDefault()} [text] to send a message to the global chat.",
                            };

                            foreach (var message in messages)
                            {
                                ServerChannelManager.SendMessageToPeerInChannel(peer.m_uid, VChatPlugin.Name, null, message);
                            }
                        }

                        // Send channel information
                        if (ValheimHelper.GetSteamIdFromPeer(peer, out ulong steamId))
                        {
                            foreach (var channelInfo in ServerChannelManager.GetChannelsForUser(steamId))
                            {
                                ServerChannelManager.SendMessageToClient_ChannelConnected(peer.m_uid, channelInfo);
                            }
                        }

                        SpawnedPlayers.Add(peer.m_uid);
                    }
                }
            }
        }
    }
}