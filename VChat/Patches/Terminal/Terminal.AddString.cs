using HarmonyLib;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), typeof(string))]
    public static class TerminalPatchAddStringToBuffer
    {
        private static bool Prefix(ref Terminal __instance, ref string text)
        {
            // Display chat window when a new message is received.
            if (VChatPlugin.Settings.ShowChatWindowOnMessageReceived)
            {
                var chat = __instance as Chat;
                if (chat != null)
                {
                    chat.m_hideTimer = 0;
                }
                VChatPlugin.ChatHideTimer = 0;
            }

            // Update the buffer manually
            if (VChatPlugin.Settings.ChatBufferSize > 300)
            {
                __instance.m_chatBuffer.Add(text);
                while (__instance.m_chatBuffer.Count > VChatPlugin.Settings.ChatBufferSize)
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
