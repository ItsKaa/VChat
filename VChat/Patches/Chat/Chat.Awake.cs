using HarmonyLib;
using UnityEngine.UI;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
    public static class ChatPatchAwake
    {
        private static void Postfix(ref Chat __instance)
        {
            if (VChatPlugin.Settings.EnableClickThroughChatWindow)
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

                // Click-through, currently only once on start-up.
                var chatWindowChildComponents = __instance.m_chatWindow.GetComponentsInChildren<Graphic>();
                foreach (var component in chatWindowChildComponents)
                {
                    if (component != null)
                    {
                        component.raycastTarget = false;
                    }
                }
            }
        }
    }
}
