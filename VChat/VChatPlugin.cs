using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VChat.Configuration;
using VChat.Data;
using VChat.Extensions;
using VChat.Helpers;
using VChat.Messages;
using VChat.Services;

namespace VChat
{
    [BepInPlugin(GUID, Name, Version)]
    [HarmonyPatch]
    public partial class VChatPlugin : BaseUnityPlugin
    {
        public const string GUID = "org.itskaa.vchat";
        public const string Name = "VChat";
        public const string Version = "1.2.1";
        public const int    NexusID = 362;
        public const string RepositoryAuthor = "ItsKaa";
        public const string RepositoryName = "VChat";
        public const string RepositoryUrl = "https://github.com/" + RepositoryAuthor + "/" + RepositoryName;

        internal static bool IsPlayerHostedServer { get; set; }
        internal static PluginSettings Settings { get; private set; }
        public static ConcurrentDictionary<long, UserMessageInfo> ReceivedMessageInfo { get; set; }
        public static List<string> MessageSendHistory { get; private set; }
        public static int MessageSendHistoryIndex { get; set; } = 0;
        public static CombinedMessageType CurrentInputChatType { get; set; }
        public static CombinedMessageType LastChatType { get; set; }
        public static float ChatHideTimer { get; set; }
        private static readonly object _commandHandlerLock = new();

        static VChatPlugin()
        {
            ReceivedMessageInfo = new ConcurrentDictionary<long, UserMessageInfo>();
            MessageSendHistory = new List<string>();
            CommandHandler = new CommandHandler();
            LastChatType = new CombinedMessageType(CustomMessageType.Global);
            CurrentInputChatType = new CombinedMessageType(LastChatType.Value);
        }

        public void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
            Settings = new PluginSettings(Config);
            InitialiseClientCommands();

            LastChatType.Set(Settings.DefaultChatChannel);
            CurrentInputChatType.Set(LastChatType);
            Log($"Initialised {Name} ({Version})");

            // Get the latest release from github and notify if there is a newer version.
            var latestReleaseVersion = GithubHelper.GetLatestGithubRelease(RepositoryAuthor, RepositoryName);
            if (!string.IsNullOrEmpty(latestReleaseVersion))
            {
                if (VersionHelper.IsNewerVersion(Version, latestReleaseVersion))
                {
                    LogWarning($"Version {latestReleaseVersion} of VChat has been released, please see {RepositoryUrl}");
                }
                else
                {
                    Log($"{Name} ({Version}) is up to date.");
                }
            }
        }

        public static Color GetTextColor(CombinedMessageType type, bool getDefault = false)
        {
            var color = Color.white;
            if (type.IsDefaultType())
            {
                switch (type.DefaultTypeValue.Value)
                {
                    case Talker.Type.Normal:
                        color = (getDefault ? null : Settings.LocalChatColor) ?? Color.white;
                        break;
                    case Talker.Type.Shout:
                        color = (getDefault ? null : Settings.ShoutChatColor) ?? Color.yellow;
                        break;
                    case Talker.Type.Whisper:
                        color = (getDefault ? null : Settings.WhisperChatColor) ?? new Color(1.0f, 1.0f, 1.0f, 0.75f);
                        break;
                }
            }
            else if(type.IsCustomType())
            {
                switch(type.CustomTypeValue.Value)
                {
                    case CustomMessageType.Global:
                        color = (getDefault ? null : Settings.GlobalChatColor) ?? new Color(0.890f, 0.376f, 0.050f);
                        break;
                }
            }
            return color;
        }

        public static string GetFormattedMessage(CombinedMessageType messageType, string user, string text)
        {
            var textColor = GetTextColor(messageType);
            var userColor = Color.Lerp(textColor, Color.black, 0.33f);
            return $"<color={userColor.ToHtmlString()}>{user}</color>: <color={textColor.ToHtmlString()}>{text}</color>";
        }

        public static bool UpdateCurrentChatTypeAndColor(InputField inputField, string text)
        {
            if (inputField != null && text != null)
            {
                bool foundCommand = false;
                var messageType = new CombinedMessageType(LastChatType.Value);

                // Attempt to look for the used chat channel if we're starting with the command prefix.
                if (text.StartsWith(CommandHandler.Prefix, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (CommandHandler.IsValidCommandString(text, PluginCommandType.SendLocalMessage))
                    {
                        messageType.Set(Talker.Type.Normal);
                        foundCommand = true;
                    }
                    else if (CommandHandler.IsValidCommandString(text, PluginCommandType.SendWhisperMessage))
                    {
                        messageType.Set(Talker.Type.Whisper);
                        foundCommand = true;
                    }
                    else if (CommandHandler.IsValidCommandString(text, PluginCommandType.SendShoutMessage))
                    {
                        messageType.Set(Talker.Type.Shout);
                        foundCommand = true;
                    }
                    else if (CommandHandler.IsValidCommandString(text, PluginCommandType.SendGlobalMessage))
                    {
                        messageType.Set(CustomMessageType.Global);
                        foundCommand = true;
                    }
                }

                // Use the default if we didn't bind /s yet.
                if ((!foundCommand || CommandHandler.Prefix != "/")
                    && text.StartsWith("/s ", StringComparison.CurrentCultureIgnoreCase))
                {
                    messageType.Set(Talker.Type.Shout);
                }

                // Update the text to the used channel in the input box.
                if (!CurrentInputChatType.Equals(messageType))
                {
                    CurrentInputChatType = messageType;
                    UpdateChatInputColor(inputField, CurrentInputChatType);
                }
                return true;
            }

            return false;
        }

        public static void UpdateChatInputColor(InputField inputField, CombinedMessageType messageType)
        {
            if (inputField?.textComponent != null)
            {
                inputField.textComponent.color = GetTextColor(messageType);
            }
        }

        public static string FormatLogMessage(object message)
        {
            return $"[{Name}] {message}";
        }

        public static void LogError(object message)
        {
            Debug.LogError(FormatLogMessage(message));
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning(FormatLogMessage(message));
        }

        public static void Log(object message)
        {
            Debug.Log(FormatLogMessage(message));
        }
    }
}
