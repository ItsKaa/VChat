using HarmonyLib;
using System;
using VChat.Data;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.AddString), typeof(string), typeof(string), typeof(Talker.Type))]
    public static class ChatPatchAddStringFormatting
    {
        private static bool Prefix(ref Chat __instance, ref string user, ref string text, ref Talker.Type type)
        {
            // Pings should not add a message to the chat.
            if (type == Talker.Type.Ping)
            {
                return false;
            }

            __instance.AddString(VChatPlugin.GetFormattedMessage(new CombinedMessageType(type), user, text));
            return false;
        }
    }

    [HarmonyPatch(typeof(Chat), nameof(Chat.AddString), typeof(string))]
    public static class ChatPatchAddStringToBuffer
    {
        private static bool Prefix(ref Chat __instance, ref string text)
        {
            // Display chat window when a new message is received.
            if (VChatPlugin.Settings.ShowChatWindowOnMessageReceived)
            {
                __instance.m_hideTimer = 0;
                VChatPlugin.ChatHideTimer = 0;
            }

            // Update the buffer manually
            var chatBufferSize = Math.Max(15, Math.Min(1000, VChatPlugin.Settings.ChatBufferSize));
            if (VChatPlugin.Settings.ChatBufferSize > chatBufferSize)
            {
                __instance.m_chatBuffer.Add(text);
                while (__instance.m_chatBuffer.Count > chatBufferSize)
                {
                    __instance.m_chatBuffer.RemoveAt(0);
                }
                __instance.UpdateChat();
                return false;
            }

            return true;
        }
    }
}
