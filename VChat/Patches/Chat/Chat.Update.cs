using HarmonyLib;
namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
    static class ChatPatchUpdate
    {
        private static void Postfix(ref Chat __instance)
        {
            if (VChatPlugin.AlwaysShowChatWindow)
            {
                __instance.m_hideTimer = 0;
            }
        }
    }
}
