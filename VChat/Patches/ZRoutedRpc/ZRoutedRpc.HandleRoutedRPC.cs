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
                // Read local say chat messages for users not connected to VChat.
                // Messages that fit the global chat command name will be redirected as global chat messages.
                if (GreetingMessage.PeerInfo.TryGetValue(data.m_senderPeerID, out GreetingMessagePeerInfo peerInfo)
                    && !peerInfo.HasReceivedGreeting)
                {
                    try
                    {
                        var senderPeer = ZNet.instance.GetPeer(data.m_senderPeerID);
                        var package = new ZPackage(data.m_parameters.GetArray());
                        var ctype = package.ReadInt();
                        var playerName = package.ReadString();
                        var text = package.ReadString();

                        if (ctype == (int)Talker.Type.Normal)
                        {
                            var globalChatCommand = VChatPlugin.CommandHandler.FindCommand(PluginCommandType.SendGlobalMessage);
                            if (VChatPlugin.CommandHandler.IsValidCommandString(text, globalChatCommand, out text))
                            {
                                VChatPlugin.Log($"Redirecting local message to global chat from peer {data.m_senderPeerID} \"({senderPeer?.m_playerName})\" with message \"{text}\".");

                                // Redirect this message to the global chat channel.
                                foreach (var peer in ZNet.instance.GetConnectedPeers())
                                {
                                    // Exclude the sender, otherwise it'd probably just be annoying.
                                    if (peer.m_uid != data.m_senderPeerID)
                                    {
                                        GlobalMessages.SendGlobalMessageToPeer(peer.m_uid, (int)GlobalMessageType.RedirectedGlobalMessage, senderPeer?.m_refPos ?? new Vector3(), senderPeer?.m_playerName ?? playerName, text);
                                    }
                                }

                                // Intercept message so that other connected users won't receive the same message twice.
                                data.m_methodHash = GlobalMessages.InterceptedSayHashCode;
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        VChatPlugin.LogError($"Error reading Talker.Say message for unconnected VChat user ({data.m_senderPeerID}): {ex}");
                    }
                }
            }

            return true;
        }
    }
}
