using HarmonyLib;
using System;
using UnityEngine;
using VChat.Data;
using VChat.Messages;
using static ZRoutedRpc;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.HandleRoutedRPC))]
    public static class ZRoutedRpcHandleRoutedRPC
    {
        public static bool Prefix(ref ZRoutedRpc __instance, ref RoutedRPCData data)
        {
            if (ZNet.m_isServer && data?.m_methodHash == GlobalMessages.TalkerSayHashCode)
            {
                try
                {
                    var senderPeer = ZNet.instance.GetPeer(data.m_senderPeerID);
                    var package = new ZPackage(data.m_parameters.GetArray());
                    var ctype = package.ReadInt();
                    var playerName = package.ReadString();
                    var text = package.ReadString();
                    var senderSteamSocket = senderPeer.m_socket as ZSteamSocket;
                    var senderSteamId = senderSteamSocket?.GetPeerID().m_SteamID ?? ulong.MaxValue;

                    VChatPlugin.Log($"Got message from  by user {playerName}: \"{text}\"");

                    bool intercept = false;

                    // Read local say chat messages for users not connected to VChat.
                    // Messages that fit the global chat command name will be redirected as global chat messages.
                    if (GreetingMessage.PeerInfo.TryGetValue(data.m_senderPeerID, out GreetingMessagePeerInfo peerInfo)
                        && !peerInfo.HasReceivedGreeting)
                    {
                        if (ctype == (int)Talker.Type.Normal)
                        {
                            var globalChatCommand = VChatPlugin.CommandHandler.FindCommand(PluginCommandType.SendGlobalMessage);
                            if (VChatPlugin.CommandHandler.IsValidCommandString(text, globalChatCommand, out text))
                            {
                                VChatPlugin.Log($"Redirecting local message to global chat from peer {data.m_senderPeerID} \"({senderPeer?.m_playerName ?? playerName})\" with message \"{text}\".");

                                // Redirect this message to the global chat channel.
                                foreach (var peer in ZNet.instance.GetConnectedPeers())
                                {
                                    // Exclude the sender if the setting is disabled.
                                    if (VChatPlugin.Settings.SendGlobalMessageConfirmationToNonVChatUsers || peer.m_uid != data.m_senderPeerID)
                                    {
                                        GlobalMessages.SendGlobalMessageToPeer(peer.m_uid, (int)GlobalMessageType.RedirectedGlobalMessage, senderPeer?.m_refPos ?? new Vector3(), senderPeer?.m_playerName ?? playerName, text);
                                    }
                                }

                                // If this is a player-hosted server, the local client will not be in the peer collection, so add that message directly to the chat.
                                if (VChatPlugin.IsPlayerHostedServer && !ZNet.instance.IsDedicated() && Chat.instance != null)
                                {
                                    VChatPlugin.Log($"(Player-hosted server) Adding message to local player chat.");
                                    var formattedMessage = VChatPlugin.GetFormattedMessage(new CombinedMessageType(CustomMessageType.Global), senderPeer?.m_playerName ?? playerName, text);
                                    Chat.instance?.AddString(formattedMessage);
                                }

                                // Send the global message to the chat instance to be compatibile with other server mods that read the chat.
                                // We can't send this message to the chat instance if we're running a player-hosted server because then a duplicate message will pop up.
                                if (VChatPlugin.Settings.EnableModCompatibility && Chat.instance != null && ZNet.instance.IsDedicated())
                                {
                                    VChatPlugin.Log($"Mod compatibility: sending global chat message as a local chat message.");
                                    Chat.instance.OnNewChatMessage(null, data.m_senderPeerID, senderPeer?.m_refPos ?? new Vector3(), Talker.Type.Normal, senderPeer?.m_playerName ?? playerName, text);
                                }

                                intercept = true;
                            }
                        }
                    }

                    if (intercept || VChatPlugin.CommandHandler.TryFindAndExecuteServerCommand(text, senderPeer, senderSteamId, out PluginCommandServer _))
                    {
                        // Intercept message so that other connected users won't receive the same message twice.
                        data.m_methodHash = GlobalMessages.InterceptedSayHashCode;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    VChatPlugin.LogError($"Error reading Talker.Say message for unconnected VChat user ({data.m_senderPeerID}): {ex}");
                }
            }

            return true;
        }
    }
}
