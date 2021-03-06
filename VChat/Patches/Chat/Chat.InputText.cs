using HarmonyLib;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.InputText))]
    public static class ChatPatchInputText
    {
        private static bool Prefix(ref Chat __instance)
        {
            var text = __instance.m_input.text;

            // Add the message to the chat history.
            if (VChatPlugin.MaxMessageSendHistoryCount > 0)
            {
                if (VChatPlugin.MessageSendHistory.Count > VChatPlugin.MaxMessageSendHistoryCount)
                {
                    VChatPlugin.MessageSendHistory.RemoveAt(0);
                }

                VChatPlugin.MessageSendHistory.Add(text);
                VChatPlugin.MessageSendHistoryIndex = 0;
            }

            if (VChatPlugin.AutoShout)
            {
                __instance.SendText(Talker.Type.Shout, text);
                return false;
            }

            return true;
        }
    }
}
