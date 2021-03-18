using HarmonyLib;
using System;
using UnityEngine;
using VChat.Data;
using VChat.Messages;
using VChat.Services;
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

                                // Intercept message so that other connected users won't receive the same message twice.
                                data.m_methodHash = GlobalMessages.InterceptedSayHashCode;
                                return false;
                            }
                        }
                    }

                    // Server-sided messages for all parties
                    if (text.Trim().StartsWith("/addchannel", StringComparison.CurrentCultureIgnoreCase))
                    {
                        VChatPlugin.LogWarning($"Got addchannel from local chat");
                        text = text.Remove(0, "/addchannel".Length);
                        var channelName = text.Trim();
                        ServerChannelManager.ClientSendAddChannelToServer(senderPeer.m_uid, senderSteamId, channelName);
                    }
                    else if (text.Trim().StartsWith("/invite", StringComparison.CurrentCultureIgnoreCase))
                    {
                        VChatPlugin.LogWarning($"Got invite from local chat");
                        text = text.Remove(0, "/invite".Length);
                        var remainder = text.Trim();
                        var remainderData = text.Split(new[] { " " }, StringSplitOptions.None);
                        if(remainderData.Length >= 2)
                        {
                            var channelName = remainderData[0];
                            var inviteePlayerName = remainderData[1];

                            var foundPeer = false;
                            foreach(var targetPeer in ZNet.instance.GetConnectedPeers())
                            {
                                if (targetPeer.m_socket is ZSteamSocket targetSteamSocket)
                                {
                                    ServerChannelManager.InvitePlayerToChannel(channelName,
                                        data.m_senderPeerID,
                                        senderSteamId,
                                        targetSteamSocket.GetPeerID().m_SteamID
                                    );
                                    foundPeer = true;
                                    break;
                                }
                            }

                            if (!foundPeer)
                            {
                                ChannelInviteMessage.SendFailedResponseToPeer(senderPeer.m_uid, ChannelInviteMessage.ChannelInviteResponseType.UserNotFound, channelName);
                            }
                        }
                    else if (text.Trim().StartsWith("/accept", StringComparison.CurrentCultureIgnoreCase))
                    {
                        VChatPlugin.LogWarning($"Got accept from local chat");
                        text = text.Remove(0, "/accept".Length);
                        var channelName = text.Trim();
                        if (string.IsNullOrEmpty(channelName))
                        {
                            var invites = ServerChannelManager.GetChannelInvitesForUser(senderSteamId);
                            if (invites?.Count() > 0)
                            {
                                ServerChannelManager.AcceptChannelInvite(data.m_senderPeerID, invites.FirstOrDefault().ChannelName);
                            }
                            else
                            {
                                ChannelInviteMessage.SendFailedResponseToPeer(senderPeer.m_uid, ChannelInviteMessage.ChannelInviteResponseType.NoInviteFound, channelName);
                            }
                        }
                        else
                        {
                            ServerChannelManager.AcceptChannelInvite(data.m_senderPeerID, channelName);
                        }
                    }
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
