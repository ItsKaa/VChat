using System;
using UnityEngine;
using VChat.Data;
using VChat.Extensions;
using VChat.Messages;
using VChat.Services;

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
        public static void SendMessageToPeer(long peerId, string channelName, string playerName, string text, string VChatMethodName, object[] VChatParameters, System.Version minPluginVersion = null, Color? color = null)
        {
            SendMessageToPeer(peerId, channelName, playerName, text, () => ZRoutedRpc.instance.InvokeRoutedRPC(peerId, VChatMethodName, VChatParameters), minPluginVersion, color);
        }

        public static void SendMessageToPeer(long peerId, string channelName, string playerName, string text, string VChatMethodName, object VChatParameter, System.Version minPluginVersion = null, Color? color = null)
            => SendMessageToPeer(peerId, channelName, playerName, text, VChatMethodName, new object[] { VChatParameter }, minPluginVersion, color);

        /// <summary>
        /// Sends a message to a peer with VChat installed or a ChatMessage to a vanilla client.
        /// </summary>
        /// <param name="playerName">The player name used when the client does not have VChat</param>
        /// <param name="text">The text message, used when the client does not have VChat</param>
        /// <param name="onVChatInstalledAction">The action that's called when VChat is not installed</param>
        public static void SendMessageToPeer(long peerId, string channelName, string playerName, string text, Action onVChatInstalledAction, System.Version minPluginVersion, Color? color = null)
        {
            if (GreetingMessage.PeerInfo.TryGetValue(peerId, out GreetingMessagePeerInfo peerInfo) && peerInfo.HasReceivedGreeting
                && (minPluginVersion == null || (!string.IsNullOrEmpty(peerInfo.Version) && new System.Version(peerInfo.Version) >= minPluginVersion)))
            {
                onVChatInstalledAction?.Invoke();
            }
            else
            {
                // VChat wasn't found on this client instance so we'll send it as a local chat message to that client.
                // Local chat has a limited range so we'll send it at the position of that user.
                var peer = ZNet.instance.GetPeer(peerId);
                if (peer != null)
                {
                    // Remove rich-text formatting because the the vanilla client doesn't support it.
                    text = text.StripRichTextFormatting();

                    // Find channel info to color the channel name
                    var channelInfo = ServerChannelManager.FindChannel(channelName);
                    var channelColor = (channelInfo?.Color ?? Color.white);

                    // Set color for global, this isn't in the custom channels list yet.
                    if (channelInfo == null && string.Equals(channelName, "Global"))
                    {
                        channelColor = VChatPlugin.GetTextColor(new CombinedMessageType(CustomMessageType.Global));
                    }

                    if(color.HasValue)
                    {
                        channelColor = color.Value;
                    }

                    // Construct parameters for the ChatMessage command and send it to the target peer.
                    var chatMessageParameters = new object[] {
                        peer.m_refPos,
                        (int)Talker.Type.Normal,
                        $"<color={channelColor.ToHtmlString()}>[{(string.IsNullOrEmpty(channelName) ? VChatPlugin.Name : channelName)}]</color>",
                        string.IsNullOrEmpty(playerName) ? text : $"{playerName}: {text}"
                    };
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, "ChatMessage", chatMessageParameters);
                }
            }
        }
    }
}
