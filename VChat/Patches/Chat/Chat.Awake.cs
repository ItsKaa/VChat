using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using VChat.Extensions;
using VChat.Helpers;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
    public static class ChatPatchAwake
    {
        private static void Prefix(ref Chat __instance)
        {
            // Use a setting to add the canvas group, just in case a future update of Valheim breaks this.
            if (VChatPlugin.Settings.UseChatOpacity)
            {
                var canvasGroup = __instance.m_chatWindow.gameObject.AddComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = false;
                }
            }

            __instance.UpdateChatSize(new Vector2(VChatPlugin.Settings.ChatWidth, VChatPlugin.Settings.ChatHeight));
        }

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
            if (VChatPlugin.Settings.EnableClickThroughChatWindow && __instance.m_chatWindow != null)
            {
                __instance.m_chatWindow.ChangeClickThroughInChildren(VChatPlugin.Settings.EnableClickThroughChatWindow);
            }
            
            // Set the hide delay.
            __instance.m_hideDelay = VChatPlugin.Settings.ChatHideDelay;

            // Update the input colour since we may not be on local.
            VChatPlugin.UpdateChatInputColor(__instance.m_input, VChatPlugin.LastChatType);

            // Get the latest release of VChat and notify the user when the chat is starting up for the first time.
            var latestReleaseVersion = GithubHelper.GetLatestGithubRelease(VChatPlugin.RepositoryAuthor, VChatPlugin.RepositoryName);
            if (!string.IsNullOrEmpty(latestReleaseVersion))
            {
                List<string> messages = new();
                if (VersionHelper.IsNewerVersion(VChatPlugin.Version, latestReleaseVersion, VChatPlugin.IsBetaVersion))
                {
                    messages.AddRange(new[] {
                        $"You are running on an older version of {VChatPlugin.Name} ({VChatPlugin.Version}{(VChatPlugin.IsBetaVersion ? " BETA" : "")}).",
                        $"version {latestReleaseVersion} has been released, see {VChatPlugin.RepositoryUrl}",
                    });
                }
                else
                {
                    messages.Add($"{VChatPlugin.Name} {VChatPlugin.Version}{(VChatPlugin.IsBetaVersion ? " BETA" : "")} is loaded and up to date.");
                }

                foreach (var msg in messages)
                {
                    __instance.AddString($"[{VChatPlugin.Name}] {msg}");
                }
            }

            // Reset the server plugin status as we may have changed server from a single player world to a dedicated server.
            PlayerPatchOnSpawned.HasPlayerSpawnedOnce = false;
        }
    }
}
