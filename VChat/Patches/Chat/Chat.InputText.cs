using HarmonyLib;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.InputText))]
    public static class ChatPatchInputText
    {
        private static bool Prefix(ref Chat __instance)
        {
            if (VChatPlugin.AutoShout)
            {
                __instance.SendText(Talker.Type.Shout, __instance.m_input.text);
                return false;
            }

            return true;
        }
    }
}
