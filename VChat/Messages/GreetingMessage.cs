using System.Collections.Concurrent;
using VChat.Data;
using VChat.Helpers;

namespace VChat.Messages
{
    public static class GreetingMessage
    {
        public const string GreetingHashName = VChatPlugin.GUID + ".greet";
        public static ConcurrentDictionary<long, GreetingMessagePeerInfo> PeerInfo { get; private set; }

        private static bool _hasLocalPlayerGreetedToServer = false;
        public static bool HasLocalPlayerGreetedToServer {
            get => VChatPlugin.IsPlayerHostedServer || _hasLocalPlayerGreetedToServer;
            private set => _hasLocalPlayerGreetedToServer = value;
        }

        private static bool _hasLocalPlayerReceivedGreetingFromServer = false;
        public static bool HasLocalPlayerReceivedGreetingFromServer {
            get => VChatPlugin.IsPlayerHostedServer || _hasLocalPlayerReceivedGreetingFromServer;
            private set => _hasLocalPlayerReceivedGreetingFromServer = value;
        }

        private static string _serverVersion = "0.0.0";
        public static string ServerVersion
        {
            get => VChatPlugin.IsPlayerHostedServer ? VChatPlugin.Version : _serverVersion;
            private set => _serverVersion = value;
        }

        static GreetingMessage()
        {
            PeerInfo = new ConcurrentDictionary<long, GreetingMessagePeerInfo>();
            Reset();
        }

        /// <summary>
        /// Register the global message commands, this should be called when ZNet initialises.
        /// </summary>
        public static void Register()
        {
            VChatPlugin.Log($"Registering custom routed messages for greetings.");
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register(GreetingHashName, new RoutedMethod<string>(OnServerMessage).m_action);
            }
            else
            {
                ZRoutedRpc.instance.Register(GreetingHashName, new RoutedMethod<string>(OnClientMessage).m_action);
            }
        }

        public static void Reset()
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
            if (senderId != ValheimHelper.GetServerPeerId())
            {
                var peer = ZRoutedRpc.instance?.GetPeer(senderId);
                if (peer != null)
                {
                    VChatPlugin.Log($"Greeting received from client \"{peer?.m_playerName}\" ({senderId}) with version {version}.");
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
                    VChatPlugin.LogWarning($"Received greeting from an unconnected peer with id {senderId}.");
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Received a greeting from a peer with the server id...");
            }
        }

        /// <summary>
        /// When the client receives a greeting from the server.
        /// This makes us aware that the server is running VChat.
        /// </summary>
        private static void OnClientMessage(long senderId, string version)
        {
            if (senderId == ValheimHelper.GetServerPeerId())
            {
                VChatPlugin.Log($"Received a greeting from the server ({senderId}) that's running on {VChatPlugin.Name} {version}.");

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
                VChatPlugin.Log($"Ignoring a greeting received from a client with id {senderId} and version {version}.");
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
                    if(previousGreeting.HasSentGreeting)
                    {
                        VChatPlugin.LogWarning($"Player \"{ZNet.instance.GetPeer(peerId)?.m_playerName}\" ({peerId}) has already been greeted, but sending anyway.");
                    }

                    peerInfo = previousGreeting;
                    peerInfo.HasSentGreeting = true;
                }
                else
                {
                    peerInfo = new GreetingMessagePeerInfo()
                    {
                        PeerId = peerId,
                        Version = "0.0.0",
                        HasReceivedGreeting = false,
                        HasSentGreeting = true,
                    };
                }

                PeerInfo.AddOrUpdate(peerId, peerInfo, (long oldKey, GreetingMessagePeerInfo oldValue) => peerInfo);
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the greeing to a client.");
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
                ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), GreetingHashName, parameters);
                HasLocalPlayerGreetedToServer = true;
            }
            else
            {
                VChatPlugin.LogError($"Cannot send the greeing to ourself, are we missing a server check?");
            }
        }
    }
}
