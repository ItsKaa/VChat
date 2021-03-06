using HarmonyLib;
using VChat.Extensions;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
    public static class ChatPatchAwake
    {
        private static void Postfix(ref Chat __instance)
        {
                var chat = __instance;

                // Listen on value changed, this updated the input field and carrot text color to match the targeted channel.
                __instance.m_input.onValueChanged.AddListener((text) =>
                {
                    if (chat.m_input != null)
                    {
                        VChatPlugin.UpdateCurrentChatTypeAndColor(chat.m_input, text);
                    }
                });

                // Listen when the chat field is closed, this resets the position of the message history (arrow up & down handler)
                __instance.m_input.onEndEdit.AddListener((text) =>
                {
                    if (string.IsNullOrEmpty(chat.m_input.text))
                    {
                        VChatPlugin.MessageSendHistoryIndex = 0;
                    }
                });

            // Enable chat window click-through.
            if (VChatPlugin.Settings.EnableClickThroughChatWindow)
            {
                __instance.m_chatWindow?.ChangeClickThroughInChildren(VChatPlugin.Settings.EnableClickThroughChatWindow);
            }
        }
    }
}
