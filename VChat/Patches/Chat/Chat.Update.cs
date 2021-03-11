using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
    static class ChatPatchUpdate
    {
        private static void Prefix(ref Chat __instance)
        {
            // Handle key inputs
            bool chatWindowFocused = __instance.m_input.isActiveAndEnabled && __instance.m_input.isFocused;
            if (chatWindowFocused)
            {
                if (VChatPlugin.MessageSendHistory.Count > 0 && VChatPlugin.Settings.MaxPlayerMessageHistoryCount > 0)
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
        }

        private static void Postfix(ref Chat __instance)
        {
            bool chatWindowFocused = __instance.m_input.isActiveAndEnabled && __instance.m_input.isFocused;

            // Reset or add seconds to the hide timer.
            if (chatWindowFocused || __instance.m_hideTimer == 0f)
            {
                VChatPlugin.ChatHideTimer = 0;
            }
            else
            {
                VChatPlugin.ChatHideTimer += Time.deltaTime;
            }

            // Chat fade-out code based on active and inactive opacity values.
            bool isHidden = false;
            float activeChatAlpha = Math.Min(VChatPlugin.Settings.ChatOpacity / 100f, 1f);
            float inactiveChatAlpha = Math.Min(VChatPlugin.Settings.InactiveChatOpacity / 100f, 1f);
            if (VChatPlugin.Settings.UseChatOpacity && !(activeChatAlpha == 1f && inactiveChatAlpha == 1f))
            {
                float maxHideTimerValue = VChatPlugin.Settings.ChatHideDelay + VChatPlugin.Settings.ChatFadeTimer;
                var canvasGroup = __instance.m_chatWindow.GetComponent<CanvasGroup>();

                // Hidden or inactive if always showing the chat
                if (VChatPlugin.ChatHideTimer >= maxHideTimerValue)
                {
                    if (VChatPlugin.Settings.AlwaysShowChatWindow)
                    {
                        // Set alpha to inactive when always showing the chat window
                        if (canvasGroup != null)
                        {
                            canvasGroup.alpha = inactiveChatAlpha;
                        }
                    }
                    else
                    {
                        isHidden = true;
                    }

                    VChatPlugin.ChatHideTimer = maxHideTimerValue;
                }
                // Fading out
                else if (VChatPlugin.ChatHideTimer > VChatPlugin.Settings.ChatHideDelay)
                {
                    var fadeTimerMax = VChatPlugin.Settings.ChatFadeTimer;
                    var fadeSecondsIn = VChatPlugin.ChatHideTimer - VChatPlugin.Settings.ChatHideDelay;
                    if (fadeSecondsIn > fadeTimerMax)
                    {
                        fadeSecondsIn = fadeTimerMax;
                    }

                    // Update alpha for the canvas group.
                    if (canvasGroup != null)
                    {
                        if (chatWindowFocused)
                        {
                            canvasGroup.alpha = activeChatAlpha;
                        }
                        else
                        {
                            // Update alpha value based on the fade-in percentage.
                            var alphaModifier = Math.Min(1f, (fadeTimerMax - fadeSecondsIn) / fadeTimerMax);
                            float alpha = activeChatAlpha * alphaModifier;

                            // Add the inactive chat window alpha so that it transitions correctly.
                            if (VChatPlugin.Settings.AlwaysShowChatWindow)
                            {
                                alpha += VChatPlugin.Settings.InactiveChatOpacity / 100f;
                            }

                            canvasGroup.alpha = Math.Min(alpha, 1f);
                        }
                    }
                }
                // Focused or simply visible.
                else if (canvasGroup != null)
                {
                    canvasGroup.alpha = (chatWindowFocused || __instance.m_hideTimer == 0f) ? 1f : activeChatAlpha;
                }

                if (isHidden)
                {
                    __instance.m_hideTimer = __instance.m_hideDelay;
                }
                else
                {
                    __instance.m_hideTimer = 0f;
                }
            }
        }
    }
}
