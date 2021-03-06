using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
    static class ChatPatchUpdate
    {
        private static void Postfix(ref Chat __instance)
        {
            if (__instance.m_input.isActiveAndEnabled && __instance.m_input.isFocused)
            {
                if (VChatPlugin.MessageSendHistory.Count > 0 && VChatPlugin.MaxMessageSendHistoryCount > 0)
                {
                    bool showMessageHistory = false;
                    // Increase or decrease the history counter when pressing up/down arrows.
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        VChatPlugin.MessageSendHistoryIndex--;
                        showMessageHistory = true;
                    }
                    else if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        VChatPlugin.MessageSendHistoryIndex++;
                        showMessageHistory = true;
                    }

                    if (showMessageHistory)
                    {
                        // Reset if it exceeds the message hsitory count.
                        if (VChatPlugin.MessageSendHistoryIndex > VChatPlugin.MessageSendHistory.Count
                            || VChatPlugin.MessageSendHistoryIndex < 0 && Math.Abs(VChatPlugin.MessageSendHistoryIndex) > VChatPlugin.MessageSendHistory.Count)
                        {
                            VChatPlugin.MessageSendHistoryIndex = 0;
                        }

                        // Index 0 will clear the input.
                        string historyText = "";
                        if (VChatPlugin.MessageSendHistoryIndex != 0)
                        {
                            historyText = VChatPlugin.MessageSendHistory.ElementAt(
                                VChatPlugin.MessageSendHistoryIndex >= 0
                                ? (VChatPlugin.MessageSendHistoryIndex - 1)
                                : (VChatPlugin.MessageSendHistoryIndex + VChatPlugin.MessageSendHistory.Count)
                            );
                        }
                        __instance.m_input.text = historyText;
                        __instance.m_input.caretPosition = historyText.Length;
                    }
                }
            }
            else if (VChatPlugin.AlwaysShowChatWindow)
            {
                __instance.m_hideTimer = 0;
            }
        }
    }
}
