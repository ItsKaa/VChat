using HarmonyLib;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.AddString), typeof(string), typeof(string), typeof(Talker.Type))]
    public static class ChatPatchAddString
    {
        private static bool Prefix(ref Chat __instance, ref string user, ref string text, ref Talker.Type type)
        {
            __instance.AddString(VChatPlugin.GetFormattedMessage(type, user, text));
            return false;
        }
    }
}
