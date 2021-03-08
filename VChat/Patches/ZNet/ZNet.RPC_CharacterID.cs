using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using VChat.Data;
using VChat.Messages;

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
                                $"This server runs {VChatPlugin.Name} {VChatPlugin.Version}, We detected that you do not have this mod installed, be aware that you will not be able to see the global chat channel.",
                                $"You can find the latest version on {VChatPlugin.Repository}"
                            };

                            foreach (var message in messages)
                            {
                                object[] parameters = new object[] { peer.GetRefPos(), (int)Talker.Type.Normal, VChatPlugin.Name, message };
                                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ChatMessage", parameters);
                            }
                        }

                        SpawnedPlayers.Add(peer.m_uid);
                    }
                }
            }
        }
    }
}
