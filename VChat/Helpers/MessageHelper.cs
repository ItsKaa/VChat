﻿using System;
using VChat.Data;
using VChat.Extensions;
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
        public static void SendMessageToPeer(long peerId, string channelName, string playerName, string text, string VChatMethodName, object[] VChatParameters, System.Version minPluginVersion = null)
        {
            SendMessageToPeer(peerId, channelName, playerName, text, () => ZRoutedRpc.instance.InvokeRoutedRPC(peerId, VChatMethodName, VChatParameters), minPluginVersion);
        }

        public static void SendMessageToPeer(long peerId, string channelName, string playerName, string text, string VChatMethodName, object VChatParameter, System.Version minPluginVersion = null)
            => SendMessageToPeer(peerId, channelName, playerName, text, VChatMethodName, new object[] { VChatParameter }, minPluginVersion);

        /// <summary>
        /// Sends a message to a peer with VChat installed or a ChatMessage to a vanilla client.
        /// </summary>
        /// <param name="playerName">The player name used when the client does not have VChat</param>
        /// <param name="text">The text message, used when the client does not have VChat</param>
        /// <param name="onVChatInstalledAction">The action that's called when VChat is not installed</param>
        public static void SendMessageToPeer(long peerId, string channelName, string playerName, string text, Action onVChatInstalledAction, System.Version minPluginVersion)
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
                    // Reformat text since the vanilla client doesn't support rich-text formatting.
                    text = text.ReplaceIgnoreCase(new[] { "<i>", "</i>", "<b>", "</b>" }, "");

                    var chatMessageParameters = new object[] {
                        peer.m_refPos,
                        (int)Talker.Type.Normal,
                        $"[{(string.IsNullOrEmpty(channelName) ? VChatPlugin.Name : channelName)}]",
                        string.IsNullOrEmpty(playerName) ? text : $"{playerName}: {text}"
                    };
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, "ChatMessage", chatMessageParameters);
                }
            }
        }
    }
}
