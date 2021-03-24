using System.Linq;
using VChat.Helpers;
using VChat.Services;

namespace VChat.Messages
{
    public static class ChannelLeaveMessage
    {
        public enum ChannelLeaveResponseType
        {
            OK,
            ChannelNotFound,
        }

        public const string ChannelCreateMessageHashName = VChatPlugin.Name + ".ChannelLeave";
        public const int Version = 1;

        static ChannelLeaveMessage()
        {
        }

        public static void Register()
        {
            if (ZNet.m_isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(ChannelCreateMessageHashName, OnMessage_Server);
            }
        }

        private static void OnMessage_Server(long senderId, ZPackage package)
        {
            if (ZNet.m_isServer)
            {
                var version = package.ReadInt();
                var channelName = package.ReadString();
                var senderPeerId = package.ReadLong();

                // Only use the packet peer id if it's sent from the server.
                if (senderId != ValheimHelper.GetServerPeerId())
                {
                    senderPeerId = senderId;
                }

                var peer = ZNet.instance?.GetPeer(senderPeerId);
                if (peer != null && peer.m_socket is ZSteamSocket steamSocket)
                {
                    LeaveChannelForPeer(senderPeerId, steamSocket.GetPeerID().m_SteamID, channelName);
                }
            }
        }

        public static void SendToPeer(long peerId, ChannelLeaveResponseType responseType, string channelName)
        {
            if (ZNet.m_isServer)
            {
                var peer = ValheimHelper.GetPeer(peerId);
                if (peer != null)
                {
                    bool isSuccess = false;
                    string text = null;
                    switch (responseType)
                    {
                        case ChannelLeaveResponseType.OK:
                            {
                                text = $"You have left the channel {channelName}.";
                                isSuccess = true;
                            }
                            break;
                        case ChannelLeaveResponseType.ChannelNotFound:
                            text = "Unable to leave a channel you haven't joined.";
                            break;
                        default:
                            VChatPlugin.LogError($"Unknown response type for leave channel received: {responseType}");
                            break;
                    }

                    if (!string.IsNullOrEmpty(text))
                    {
                        if (isSuccess)
                        {
                            ServerChannelManager.SendVChatSuccessMessageToPeer(peerId, text);
                        }
                        else
                        {
                            ServerChannelManager.SendVChatErrorMessageToPeer(peerId, text);
                        }
                    }
                }
            }
            else
            {
                VChatPlugin.LogWarning($"Cannot send the channel create response to a client.");
            }
        }

        public static void SendToServer(long senderPeerId, string channelName)
        {
            var package = new ZPackage();
            package.Write(Version);
            package.Write(channelName);
            package.Write(senderPeerId);

            ZRoutedRpc.instance.InvokeRoutedRPC(ValheimHelper.GetServerPeerId(), ChannelCreateMessageHashName, package);
        }

        private static bool LeaveChannelForPeer(long senderPeerId, long targetPeerId, ulong targetSteamId, string channelName)
        {
            if (ZNet.m_isServer)
            {
                if (ServerChannelManager.DoesChannelExist(channelName))
                {
                    var channel = ServerChannelManager.GetChannelsForUser(targetSteamId).FirstOrDefault(x => string.Equals(x.Name, channelName, System.StringComparison.CurrentCultureIgnoreCase));
                    if(channel != null)
                    {
                        if(ServerChannelManager.RemovePlayerFromChannel(senderPeerId, targetPeerId, targetSteamId, channelName))
                        {
                            SendToPeer(targetPeerId, ChannelLeaveResponseType.OK, channelName);
                        }
                    }
                }
                else
                {
                    SendToPeer(targetPeerId, ChannelLeaveResponseType.ChannelNotFound, channelName);
                }
            }
            return false;
        }
    }
}
