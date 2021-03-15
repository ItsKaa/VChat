using System;
using UnityEngine;
using VChat.Data;

namespace VChat.Messages
{
    public static class GlobalMessages
    {
        public const string GlobalChatHashName = VChatPlugin.GUID + ".globalchat";
        public static readonly int TalkerSayHashCode = "Say".GetStableHashCode();
        public static readonly int InterceptedSayHashCode = $"{VChatPlugin.GUID}.interceptedsay".GetStableHashCode();

        /// <summary>
        /// Register the global message commands, this should be called when ZNet initialises.
        /// </summary>
        public static void Register()
        {
            VChatPlugin.Log($"Registering custom routed messages for global chat.");
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register(GlobalChatHashName, new RoutedMethod<Vector3, int, string, string>(OnGlobalMessage_Server).m_action);
            }
            else
            {
                ZRoutedRpc.instance.Register(GlobalChatHashName, new RoutedMethod<Vector3, int, string, string>(OnGlobalMessage_Client).m_action);
            }
        }

        /// <summary>
        /// Triggered when the server receives a global message from a client .
        /// </summary>
        /// <remarks>Player name and position are primarily here for future work, player aliases for example.</remarks>
        /// <param name="senderId">The peer id of the sender</param>
        /// <param name="pos">The reported position</param>
        /// <param name="type">Reserved for future use</param>
        /// <param name="callerName">The reported player name</param>
        /// <param name="text">the message, without a playername or formatting.</param>
        private static void OnGlobalMessage_Server(long senderId, Vector3 pos, int type, string callerName, string text)
        {
            // Accept messages from the server when it's a player-hosted instance (non-dedicated), otherwise ignore.
            var serverPeerId = VChatPlugin.GetServerPeerId();
            if (senderId != serverPeerId || (VChatPlugin.IsPlayerHostedServer && senderId == serverPeerId))
            {
                var globalMessageType = (GlobalMessageType)type;
                if (globalMessageType == GlobalMessageType.StandardMessage || globalMessageType == GlobalMessageType.RedirectedGlobalMessage)
                {
                    try
                    {
                        // Sender should always be found but who knows what can happen within a few milliseconds, though I bet its still cached should that player disconnect.. safety first.
                        // We simply apply the position and player name the server knows rather than the reported values first.
                        var peer = ZRoutedRpc.instance?.GetPeer(senderId);
                        if (peer?.m_server == false || (VChatPlugin.IsPlayerHostedServer && senderId == serverPeerId))
                        {
                            // Loop through every connected peer and redirect the received message, including the original sender because the code is currently set so that the client knows that it's been sent.
                            var peers = ZNet.instance?.GetConnectedPeers();
                            if (peers != null)
                            {
                                foreach (var connectedPeer in peers)
                                {
                                    if (connectedPeer != null && !connectedPeer.m_server && connectedPeer.IsReady() && connectedPeer.m_socket?.IsConnected() == true)
                                    {
                                        VChatPlugin.Log($"Routing global message to peer {connectedPeer.m_uid} \"({connectedPeer.m_playerName})\" with message \"{text}\".");
                                        SendGlobalMessageToPeer(connectedPeer.m_uid, type, peer?.m_refPos ?? pos, peer?.m_playerName ?? callerName, text);
                                    }
                                }
                            }
                        }
                        else
                        {
                            VChatPlugin.LogWarning($"Received a global chat message from a peer identified as a server, id {senderId} \"{peer.m_playerName}\"");
                        }

                        // If this is a player-hosted server, the local client will not be in the peer collection, but will be defined as the local player.
                        if (VChatPlugin.IsPlayerHostedServer && !ZNet.instance.IsDedicated())
                        {
                            // Be sure to send it as the server, otherwise it may be ignored.
                            VChatPlugin.Log($"Redirecting player-hosted server message to local player.");
                            OnGlobalMessage_Client(serverPeerId, pos, type, callerName, text);
                        }
                    }
                    catch (Exception ex)
                    {
                        VChatPlugin.LogError($"Failed to InvokeRoutedRPC for global message ({senderId}|{text}): {ex}");
                    }
                }
                else
                {
                    VChatPlugin.LogWarning($"A global message type with value of {type} could not be parsed. Please check if there are any updates available.");
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Received a global message from a peer with the server id...");
            }
        }

        /// <summary>
        /// Triggered when the client receives a global message from the server.
        /// </summary>
        /// <remarks>Player name and position are primarily here for future work, player aliases for example.</remarks>
        /// <param name="senderId">The peer id of the sender</param>
        /// <param name="pos">The position</param>
        /// <param name="type">Reserved for future use</param>
        /// <param name="playerName">The player name</param>
        /// <param name="text">the message, without a playername or formatting.</param>
        private static void OnGlobalMessage_Client(long senderId, Vector3 pos, int type, string playerName, string text)
        {
            // If the client is connected to a server-sided instance of VChat then only accept messages from the server.
            if (!GreetingMessage.HasLocalPlayerReceivedGreetingFromServer || senderId == VChatPlugin.GetServerPeerId())
            {
                VChatPlugin.Log($"Received a global message from {playerName} ({senderId}) on location {pos} with message \"{text}\".");
                if (Chat.instance != null)
                {
                    var formattedMessage = VChatPlugin.GetFormattedMessage(new CombinedMessageType(CustomMessageType.Global), playerName, text);
                    Chat.instance?.AddString(formattedMessage);
                }
                else
                {
                    VChatPlugin.LogWarning($"Received a message but Chat instance is undefined.");
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Ignoring a global message received from a client, reported values: {senderId} \"{playerName}\" on location {pos} with message \"{text}\".");
            }
        }

        /// <summary>
        /// Send a global message to a peer, please do not use <see cref="ZRoutedRpc.Everybody"/> as warnings will be logged.
        /// </summary>
        public static void SendGlobalMessageToPeer(long peerId, int type, Vector3 pos, string playerName, string text)
        {
            // True by default as a precaution.
            bool shouldSendGlobalMessage = true;
            if (GreetingMessage.PeerInfo.TryGetValue(peerId, out GreetingMessagePeerInfo peerInfo))
            {
                shouldSendGlobalMessage = peerInfo.HasReceivedGreeting;
            }

            if (shouldSendGlobalMessage)
            {
                var parameters = new object[] { pos, type, playerName, text };
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, GlobalChatHashName, parameters);
            }
            else
            {
                // VChat wasn't found on this client instance so we'll send it as a local chat message to that client.
                // Local chat has a limited range so we'll send it at the position of that user.
                var peer = ZNet.instance.GetPeer(peerId);
                if (peer != null)
                {
                    var parameters = new object[] { peer.m_refPos, (int)Talker.Type.Normal, "[Global]", $"{playerName}: {text}"};
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, "ChatMessage", parameters);
                }
            }
        }

        /// <summary>
        /// Send a global message to the server.
        /// </summary>
        public static void SendGlobalMessageToServer(string text, GlobalMessageType type = GlobalMessageType.StandardMessage)
        {
            var localPlayer = Player.m_localPlayer;
            if (localPlayer != null)
            {
                long peerId = ZRoutedRpc.Everybody;

                if (VChatPlugin.IsPlayerHostedServer)
                {
                    peerId = VChatPlugin.GetServerPeerId();
                }
                else
                {
                    // If server-sided VChat is found, send the message directly to the server.
                    var serverPeer = ZNet.instance?.GetServerPeer();
                    if (GreetingMessage.HasLocalPlayerReceivedGreetingFromServer && serverPeer != null)
                    {
                        peerId = serverPeer.m_uid;
                    }
                }

                var parameters = new object[] { localPlayer.GetHeadPoint(), (int)type, localPlayer.GetPlayerName(), text };
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, GlobalChatHashName, parameters);
            }
            else
            {
                VChatPlugin.LogError($"Could not send global message because the player is undefined (text: \"{text}\").");
            }
        }
    }
}
