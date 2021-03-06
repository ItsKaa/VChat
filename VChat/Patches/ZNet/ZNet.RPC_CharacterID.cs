using HarmonyLib;
using System;
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
                        System.Version detectedPluginVersion = null;
                        bool isPlayerConnectedWithVChat = false;
                        if (GreetingMessage.PeerInfo.TryGetValue(peer.m_uid, out GreetingMessagePeerInfo peerInfo))
                        {
                            isPlayerConnectedWithVChat = peerInfo.HasReceivedGreeting;
                            detectedPluginVersion = new System.Version(peerInfo.Version);
                        }

                        // Send a local chat message to the player if it does not have VChat installed.
                        var messagesToSend = new List<string>();
                        if (!isPlayerConnectedWithVChat)
                        {
                            VChatPlugin.Log($"Player \"{peer.m_playerName}\" ({peer.m_uid}) spawned that does not have VChat installed, sending mod information...");

                            messagesToSend.AddRange(new[]
                            {
                                $"This server runs {VChatPlugin.Name} {VChatPlugin.Version}{(VChatPlugin.IsBetaVersion ? " BETA" : "")}, We detected that you do not have this mod installed.",
                                $"You can find the latest version on {VChatPlugin.RepositoryUrl}",
                                $"{VChatPlugin.Name} channel messages will be sent in your local chat channel.",
                                $"Type {VChatPlugin.Settings.CommandPrefix}{VChatPlugin.Settings.GlobalChatCommandName.FirstOrDefault()} [text] to send a message to the global chat.",
                            });
                        }
                        else
                        {
                            // Notify user if Version is less than 2.0.0, which doesn't support custom channels.
                            try
                            {
                                if (string.IsNullOrEmpty(peerInfo.Version) || new System.Version(peerInfo.Version) < new System.Version(2, 0, 0))
                                {
                                    messagesToSend.AddRange(new[]
                                    {
                                    $"You are running on an older version of {VChatPlugin.Name}, please be aware that custom channels will appear in your local chat channel.",
                                        $"Please update to {VChatPlugin.Name} 2.0.0 to allow the Valheim client to support these channels.",
                                    });
                                }
                            }
                            catch { }
                        }

                        foreach (var message in messagesToSend)
                        {
                            ServerChannelManager.SendMessageToPeerInChannel(peer.m_uid, VChatPlugin.Name, null, message);
                        }

                        if (ValheimHelper.GetSteamIdFromPeer(peer, out ulong steamId))
                        {
                            // Send channel information
                            foreach (var channelInfo in ServerChannelManager.GetChannelsForUser(steamId))
                            {
                                ServerChannelManager.SendMessageToClient_ChannelConnected(peer.m_uid, channelInfo);
                            }

                            // Add the player name to the colleciton
                            string playerName = peer.m_playerName;

                            VChatPlugin.KnownPlayers.AddOrUpdate(steamId,
                                (id) =>
                                {
                                    VChatPlugin.LogWarning($"Recorded player name '{playerName}' for id {steamId}.");
                                    return new KnownPlayerData()
                                    {
                                        SteamId = steamId,
                                        LastLoginTime = DateTime.UtcNow,
                                        PlayerName = playerName,
                                        LastKnownVChatVersionString = peerInfo.Version
                                    };
                                },
                                (id, oldValue) =>
                                {
                                    oldValue.UpdatePlayerName(playerName);
                                    oldValue.SteamId = steamId;
                                    oldValue.LastLoginTime = DateTime.UtcNow;
                                    oldValue.LastKnownVChatVersionString = peerInfo.Version;
                                    return oldValue;
                                });
                        }


                        SpawnedPlayers.Add(peer.m_uid);
                    }
                }
            }
        }
    }
}