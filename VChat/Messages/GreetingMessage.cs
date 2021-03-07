using System;
using System.Collections.Concurrent;
using UnityEngine;
using VChat.Data;

namespace VChat.Messages
{
    public class GreetingMessage
    {
        public const string GreetingHashName = VChatPlugin.GUID + ".greet";
        public static readonly int GreetingHashCode = GreetingHashName.GetHashCode();
        public static ConcurrentDictionary<long, GreetingMessagePeerInfo> PeerInfo { get; private set; }
        public static bool HasLocalPlayerGreetedToServer { get; private set; }
        public static bool HasLocalPlayerReceivedGreetingFromServer { get; private set; }
        public static string ServerVersion { get; private set; }

        static GreetingMessage()
        {
            PeerInfo = new ConcurrentDictionary<long, GreetingMessagePeerInfo>();
            ResetClientVariables();
        }

        /// <summary>
        /// Register the global message commands, this should be called when ZNet initialises.
        /// </summary>
        public static void Register()
        {
            Debug.Log($"Registering custom routed messages for global chat.");
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register(GreetingHashName, new RoutedMethod<string>(OnServerMessage).m_action);
            }
            else
            {
                ZRoutedRpc.instance.Register(GreetingHashName, new RoutedMethod<string>(OnClientMessage).m_action);
            }
        }

        public static void ResetClientVariables()
        {
            HasLocalPlayerGreetedToServer = false;
            HasLocalPlayerReceivedGreetingFromServer = false;
            ServerVersion = "0.0.0";
        }

        /// <summary>
        /// When the server receives a greeting from the client.
        /// This makes us aware that the user has VChat installed.
        /// </summary>
        private static void OnServerMessage(long senderId, string version)
        {
            if (senderId != ZRoutedRpc.instance.GetServerPeerID())
            {
                var peer = ZRoutedRpc.instance?.GetPeer(senderId);
                if (peer != null)
                {
                    Debug.Log($"Greeting received from client {senderId} \"{peer?.m_playerName}\" with version {version}.");
                    GreetingMessagePeerInfo peerInfo;
                    if (PeerInfo.TryGetValue(senderId, out GreetingMessagePeerInfo previousGreeting))
                    {
                        peerInfo = previousGreeting;
                        peerInfo.Version = version;
                        peerInfo.HasReceivedGreeting = true;
                    }
                    else
                    {
                        peerInfo = new GreetingMessagePeerInfo()
                        {
                            PeerId = senderId,
                            Version = version,
                            HasReceivedGreeting = true,
                            HasSentGreeting = false,
                        };
                    }

                    PeerInfo.AddOrUpdate(senderId, peerInfo, (long oldKey, GreetingMessagePeerInfo oldValue) => peerInfo);
                }
                else
                {
                    Debug.LogWarning($"Received greeting from an unconnected peer with id {senderId}.");
                }
            }
            else
            {
                Debug.LogWarning($"Received a greeting from a peer with the server id...");
            }
        }

        /// <summary>
        /// When the client receives a greeting from the server.
        /// This makes us aware that the server is running VChat.
        /// </summary>
        private static void OnClientMessage(long senderId, string version)
        {
            if (senderId == ZRoutedRpc.instance.GetServerPeerID())
            {
                Debug.LogWarning($"Received a greeting from the server {senderId} which is running on {VChatPlugin.Name} {version}.");

                // Property to determine if both sides have VChat installed.
                if(!HasLocalPlayerReceivedGreetingFromServer)
                {
                    HasLocalPlayerReceivedGreetingFromServer = true;
                    ServerVersion = version;
                }

                // Send a response to the server if we haven't yet done so.
                if (!HasLocalPlayerGreetedToServer)
                {
                    SendToServer();
                    HasLocalPlayerGreetedToServer = true;
                }
            }
            else
            {
                Debug.LogWarning($"Ignoring a greeting received from a client with id {senderId} and version {version}.");
            }
        }

        /// <summary>
        /// Send a VChat greeting message to a client, this should only be called on a server.
        /// </summary>
        public static void SendToClient(long peerId)
        {
            if (ZNet.m_isServer)
            {
                var parameters = new object[] { VChatPlugin.Version };
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, GreetingHashName, parameters);

                // Add or update peer info
                GreetingMessagePeerInfo peerInfo;
                if (PeerInfo.TryGetValue(peerId, out GreetingMessagePeerInfo previousGreeting))
                {
                    Debug.Log($"Peer has already been greeted but sending anyway.");
                    peerInfo = previousGreeting;
                    peerInfo.HasSentGreeting = true;
                }
                else
                {
                    peerInfo = new GreetingMessagePeerInfo()
                    {
                        PeerId = peerId,
                        Version = null,
                        HasReceivedGreeting = false,
                        HasSentGreeting = true,
                    };
                }

                PeerInfo.AddOrUpdate(peerId, peerInfo, (long oldKey, GreetingMessagePeerInfo oldValue) => peerInfo);
            }
            else
            {
                Debug.LogWarning($"Could not send the greeing to a client.");
            }
        }

        /// <summary>
        /// Send a VChat greeting message to the server
        /// </summary>
        public static void SendToServer()
        {
            if (!ZNet.m_isServer)
            {
                var parameters = new object[] { VChatPlugin.Version };
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), GreetingHashName, parameters);
                HasLocalPlayerGreetedToServer = true;
            }
            else
            {
                Debug.LogWarning($"Could not send the greeing to ourself.");
            }
        }
    }
}
