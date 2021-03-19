using VChat.Data;
using VChat.Messages;

namespace VChat.Helpers
{
    public static class MessageHelper
    {
        /// <summary>
        /// Sends a message to a peer with VChat installed or a ChatMessage to a vanilla client.
        /// </summary>
        /// <param name="playerName">The player name used when the client does not have VChat</param>
        /// <param name="text">The text message, used when the client does not have VChat</param>
        /// <param name="VChatMethodName">The method of the message sent to the VChat client</param>
        /// <param name="VChatParameters">The parameters for the method sent to a VChat client</param>
        public static void SendMessageToPeer(long peerId, string playerName, string text, string VChatMethodName, params object[] VChatParameters)
        {
            if (GreetingMessage.PeerInfo.TryGetValue(peerId, out GreetingMessagePeerInfo peerInfo) && peerInfo.HasReceivedGreeting)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, VChatMethodName, VChatParameters);
            }
            else
            {
                // VChat wasn't found on this client instance so we'll send it as a local chat message to that client.
                // Local chat has a limited range so we'll send it at the position of that user.
                var peer = ZNet.instance.GetPeer(peerId);
                if (peer != null)
                {
                    var chatMessageParameters = new object[] { peer.m_refPos, (int)Talker.Type.Normal, "[Global]", $"{playerName}: {text}" };
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, "ChatMessage", chatMessageParameters);
                }
            }
        }
    }
}
