using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VChat.Configuration;
using VChat.Data;
using VChat.Data.Messages;
using VChat.Extensions;
using VChat.Helpers;
using VChat.Services;

namespace VChat
{
    [BepInPlugin(GUID, Name, Version)]
    [HarmonyPatch]
    public partial class VChatPlugin : BaseUnityPlugin
    {
        public const string GUID = "org.itskaa.vchat";
        public const string Name = "VChat";
        public const string Version = "2.0.0";
        public const bool   IsBetaVersion = false;
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
        public static ServerChannelInfo CurrentCustomChatChannelInfo { get; set; }
        public static ServerChannelInfo LastCustomChatChannelInfo { get; set; }
        internal static ConcurrentDictionary<ulong, KnownPlayerData> KnownPlayers { get; set; }

        public delegate void OnInitialisedEventhandler();
        public static event OnInitialisedEventhandler OnInitialised;

        public static float ChatHideTimer { get; set; }
        private static readonly object _commandHandlerLock = new();
        private static Harmony _harmony;

        static VChatPlugin()
        {
            ReceivedMessageInfo = new ConcurrentDictionary<long, UserMessageInfo>();
            MessageSendHistory = new List<string>();
            CommandHandler = new CommandHandler();
            LastCustomChatChannelInfo = null;
            CurrentCustomChatChannelInfo = null;
            LastChatType = new CombinedMessageType(CustomMessageType.Global);
            CurrentInputChatType = new CombinedMessageType(LastChatType.Value);
            KnownPlayers = new ConcurrentDictionary<ulong, KnownPlayerData>();
        }

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);

            // VChat 2.0.0: Uses VChat.cfg instead of org.itskaa.vchat.cfg
            try
            {
                var configFilePath = Utility.CombinePaths((Application.isEditor ? "." : Paths.ConfigPath), $"{Name}.cfg");
                try
                {
                    if (File.Exists(Config.ConfigFilePath) && !File.Exists(configFilePath))
                    {
                        Log($"Updating configuration path to {configFilePath}.");
                        File.Move(Config.ConfigFilePath, configFilePath);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to move old configuration ({Config?.ConfigFilePath}) to new file path ({configFilePath}) Error: {ex}");
                }

                Settings = new PluginSettings(this, configFilePath);
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialise config with new settings path, using old path... Error: {ex}");
                Settings = new PluginSettings(Config);
            }


            InitialiseClientCommands();

            LastChatType.Set(Settings.DefaultChatChannel);
            CurrentInputChatType.Set(LastChatType);
            Log($"Initialised {Name} ({Version}) {(IsBetaVersion ? "BETA" : "")}");

            // Get the latest release from github and notify if there is a newer version.
            var latestReleaseVersion = GithubHelper.GetLatestGithubRelease(RepositoryAuthor, RepositoryName);
            if (!string.IsNullOrEmpty(latestReleaseVersion))
            {
                if (VersionHelper.IsNewerVersion(Version, latestReleaseVersion, IsBetaVersion))
                {
                    LogWarning($"Version {latestReleaseVersion} of VChat has been released, please see {RepositoryUrl}");
                }
                else
                {
                    Log($"{Name} ({Version}) is up to date.");
                }
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll(GUID);
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
                    case CustomMessageType.CustomServerChannel:
                        color = (CurrentCustomChatChannelInfo ?? LastCustomChatChannelInfo)?.Color ?? Color.white;
                        break;
                }
            }
            return color;
        }

        public static string GetFormattedMessage(CombinedMessageType messageType, string user, string text)
        {
            var textColor = GetTextColor(messageType);
            return GetFormattedMessage(textColor, null, user, text);
        }

        public static string GetFormattedMessage(Color color, string channelName, string user, string text)
        {
            var textColor = color;
            var userColor = Color.Lerp(textColor, Color.black, 0.33f);
            return $"<color={userColor.ToHtmlString()}>{(string.IsNullOrEmpty(channelName) ? "" : $"[{channelName}] ")}" +
                $"{(string.IsNullOrEmpty(user) ? "" : $"{user}")}</color>{(string.IsNullOrEmpty(user) ? "" : ": ")}<color={textColor.ToHtmlString()}>{text}</color>";
        }

        public static bool UpdateCurrentChatTypeAndColor(InputField inputField, string text)
        {
            if (inputField != null && text != null)
            {
                bool foundCommand = false;
                var isCustomChannelChanged = false;
                var messageType = new CombinedMessageType(LastChatType.Value);

                // Attempt to look for the used chat channel if we're starting with the command prefix.
                if (text.StartsWith(Settings.CommandPrefix, StringComparison.CurrentCultureIgnoreCase))
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
                    else
                    {
                        if (CommandHandler.TryFindCommand(text, out PluginCommandBase command, out string _)
                            && command is PluginCommandServerChannel serverCommand)
                        {
                            messageType.Set(CustomMessageType.CustomServerChannel);
                            foundCommand = true;

                            // Since custom is a type for multiple channels, we'll have to notify this method
                            // to force update the color when changing from one custom channel to another.
                            if (!Equals(LastCustomChatChannelInfo, serverCommand.ChannelInfo))
                            {
                                CurrentCustomChatChannelInfo = serverCommand.ChannelInfo;
                                isCustomChannelChanged = true;
                            }
                        }
                    }
                }

                // Reset last custom channel if we can't find a command anymore.
                // This happens when we type /ChannelName1 and then remove the slash, which will mean we want to color the input back to the previous channel.
                if (!foundCommand && CurrentCustomChatChannelInfo != null && CurrentInputChatType.Equals(CustomMessageType.CustomServerChannel))
                {
                    CurrentCustomChatChannelInfo = null;
                    isCustomChannelChanged = true;
                }

                // Reset if the message starts with a slash (default commands) or the predefined VChat prefix.
                if (!foundCommand && (text.StartsWith("/") || text.StartsWith(Settings.CommandPrefix)))
                {
                    CurrentCustomChatChannelInfo = null;
                    messageType.Set(Talker.Type.Normal);
                }

                // Use the default if we didn't bind /s yet.
                if ((!foundCommand || Settings.CommandPrefix != "/")
                    && text.StartsWith("/s ", StringComparison.CurrentCultureIgnoreCase))
                {
                    messageType.Set(Talker.Type.Shout);
                }

                // Update the text to the used channel in the input box.
                if (!CurrentInputChatType.Equals(messageType) || isCustomChannelChanged)
                {
                    CurrentInputChatType.Set(messageType);
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

        internal static void TriggerInitialisedEvent()
        {
            OnInitialised?.Invoke();
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
