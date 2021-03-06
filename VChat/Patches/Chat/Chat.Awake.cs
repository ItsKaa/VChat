using HarmonyLib;
using UnityEngine.UI;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
    public static class ChatPatchAwake
    {
        private static void Postfix(ref Chat __instance)
        {
            if (VChatPlugin.EnableClickThroughChatWindow)
            {
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
