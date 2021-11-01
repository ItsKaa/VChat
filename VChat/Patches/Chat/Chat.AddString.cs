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

}
