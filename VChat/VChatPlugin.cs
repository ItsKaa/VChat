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
    public class VChatPlugin : BaseUnityPlugin
    {
        public const string GUID = "org.itskaa.vchat";
        public const string Name = "VChat";
        public const string Version = "1.1.0";
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
        public static CommandHandler CommandHandler { get; set; }
        public static float ChatHideTimer { get; set; }

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
            InitialiseCommands();

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

        private void InitialiseCommands()
        {
            CommandHandler.ClearCommands();

            const string changedColorMessageSuccess = "Changed the {0} color to <color={1}>{2}</color>.";
            const string errorParseColorMessage = "Could not parse the color \"{0}\".";
            const string errorParseNumber = "Could not convert \"{0}\" to a valid number.";

            var writeErrorMessage = new Action<string>((string message) =>
            {
                Chat.instance.AddString($"<color=red>[{Name}][Error] {message}</color>");
            });

            var writeSuccessMessage = new Action<string>((string message) =>
            {
                Chat.instance.AddString($"<color=#23ff00>[{Name}] {message}</color>");
            });

            var applyChannelColorCommand = new Func<string, CombinedMessageType, Color?>((string text, CombinedMessageType messageType) =>
            {
                text = text?.Trim();
                var color = text?.ToColor();

                // Get default color if text is empty.
                if (string.IsNullOrEmpty(text))
                {
                    text = "default";
                    color = GetTextColor(messageType, true);
                }

                // Write the response message.
                if (color != null)
                {
                    writeSuccessMessage(string.Format(changedColorMessageSuccess, messageType.ToString().ToLower(), color?.ToHtmlString(), text));
                }
                else
                {
                    writeErrorMessage(string.Format(errorParseColorMessage, text));
                }
                return color;
            });


            CommandHandler.AddCommands(
                new PluginCommand(PluginCommandType.SendLocalMessage, Settings.LocalChatCommandName, (text, instance) =>
                {
                    LastChatType.Set(Talker.Type.Normal);
                    if (!string.IsNullOrEmpty(text))
                    {
                        ((Chat)instance).SendText(Talker.Type.Normal, text);
                    }
                }),
                new PluginCommand(PluginCommandType.SendShoutMessage, Settings.ShoutChatCommandName, (text, instance) =>
                {
                    LastChatType.Set(Talker.Type.Shout);
                    if (!string.IsNullOrEmpty(text))
                    {
                        ((Chat)instance).SendText(Talker.Type.Shout, text);
                    }
                }),
                new PluginCommand(PluginCommandType.SendWhisperMessage, Settings.WhisperChatCommandName, (text, instance) =>
                {
                    LastChatType.Set(Talker.Type.Whisper);
                    if (!string.IsNullOrEmpty(text))
                    {
                        ((Chat)instance).SendText(Talker.Type.Whisper, text);
                    }
                }),
                new PluginCommand(PluginCommandType.SendGlobalMessage, Settings.GlobalChatCommandName, (text, instance) =>
                {
                    LastChatType.Set(CustomMessageType.Global);
                    if (!string.IsNullOrEmpty(text))
                    {
                        GlobalMessages.SendGlobalMessageToServer(text);
                    }
                }),
                new PluginCommand(PluginCommandType.SetLocalColor, Settings.SetLocalChatColorCommandName, (text, instance) =>
                {
                    var color = applyChannelColorCommand(text, new CombinedMessageType(Talker.Type.Normal));
                    if (color != null)
                    {
                        Settings.LocalChatColor = color;
                    }
                }),
                new PluginCommand(PluginCommandType.SetShoutColor, Settings.SetShoutChatColorCommandName, (text, instance) =>
                {
                    var color = applyChannelColorCommand(text, new CombinedMessageType(Talker.Type.Shout));
                    if (color != null)
                    {
                        Settings.ShoutChatColor = color;
                    }
                }),
                new PluginCommand(PluginCommandType.SetWhisperColor, Settings.SetWhisperChatColorCommandName, (text, instance) =>
                {
                    var color = applyChannelColorCommand(text, new CombinedMessageType(Talker.Type.Whisper));
                    if (color != null)
                    {
                        Settings.WhisperChatColor = color;
                    }
                }),
                new PluginCommand(PluginCommandType.SetGlobalColor, Settings.GlobalWhisperChatColorCommandName, (text, instance) =>
                {
                    var color = applyChannelColorCommand(text, new CombinedMessageType(CustomMessageType.Global));
                    if (color != null)
                    {
                        Settings.GlobalChatColor = color;
                    }
                }),
                new PluginCommand(PluginCommandType.ToggleShowChatWindow, "showchat", (text, instance) =>
                {
                    Settings.AlwaysShowChatWindow = !Settings.AlwaysShowChatWindow;
                    writeSuccessMessage($"{(Settings.AlwaysShowChatWindow ? "Always displaying" : "Auto hiding")} chat window.");
                }),
                new PluginCommand(PluginCommandType.ToggleShowChatWindowOnMessage, Settings.ShowChatOnMessageCommandName, (text, instance) =>
                {
                    Settings.ShowChatWindowOnMessageReceived = !Settings.ShowChatWindowOnMessageReceived;
                    writeSuccessMessage($"{(Settings.ShowChatWindowOnMessageReceived ? "Displaying" : "Not displaying")} chat window when receiving a message.");
                }),
                new PluginCommand(PluginCommandType.ToggleChatWindowClickThrough, Settings.ChatClickThroughCommandName, (text, instance) =>
                {
                    Settings.EnableClickThroughChatWindow = !Settings.EnableClickThroughChatWindow;
                    writeSuccessMessage($"{(Settings.EnableClickThroughChatWindow ? "Enabled" : "Disabled")} clicking through the chat window.");
                    ((Chat)instance).m_chatWindow?.ChangeClickThroughInChildren(!Settings.EnableClickThroughChatWindow);
                }),
                new PluginCommand(PluginCommandType.SetMaxPlayerHistory, Settings.MaxPlayerChatHistoryCommandName, (text, instance) =>
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        writeSuccessMessage($"The number of stored player messages is set to {Settings.MaxPlayerMessageHistoryCount}.");
                    }
                    else
                    {
                        if (ushort.TryParse(text, out ushort value))
                        {
                            Settings.MaxPlayerMessageHistoryCount = value;
                            if (value > 0)
                            {
                                writeSuccessMessage($"Changed the maximum stored player messages to {value}.");
                            }
                            else
                            {
                                writeSuccessMessage($"Disabled capturing player chat history.");
                                MessageSendHistory.Clear();
                            }

                            // Readjust buffer size
                            while (MessageSendHistory.Count > 0 && MessageSendHistory.Count < Settings.MaxPlayerMessageHistoryCount)
                            {
                                MessageSendHistory.RemoveAt(0);
                            }
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    }
                }),
                new PluginCommand(PluginCommandType.SetHideDelay, Settings.SetChatHideDelayCommandName, (text, instance) =>
                {
                    if (float.TryParse(text, out float delay) && !float.IsNaN(delay))
                    {
                        if (delay > 0)
                        {
                            Settings.ChatHideDelay = delay;
                            ((Chat)instance).m_hideDelay = delay;
                            writeSuccessMessage($"Updated the chat hide delay to {delay} seconds.");
                        }
                        else
                        {
                            writeErrorMessage($"Hide delay must be greater than 0.");
                        }
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseNumber, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetFadeTime, Settings.SetChatFadeTimeCommandName, (text, instance) =>
                {
                    if (float.TryParse(text, out float time) && !float.IsNaN(time))
                    {
                        time = Math.Max(0f, time);
                        writeSuccessMessage($"Updated the chat fade timer to {time} seconds.");
                        Settings.ChatFadeTimer = time;
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseNumber, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetActiveOpacity, Settings.SetOpacityCommandName, (text, instance) =>
                {
                    if (uint.TryParse(text, out uint opacity))
                    {
                        opacity = Math.Min(100, Math.Max(0, opacity));
                        writeSuccessMessage($"Updated the chat opacity to {opacity}.");
                        Settings.ChatOpacity = opacity;
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseNumber, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetInactiveOpacity, Settings.SetInactiveOpacityCommandName, (text, instance) =>
                {
                    if (uint.TryParse(text, out uint opacity))
                    {
                        opacity = Math.Min(100, Math.Max(0, opacity));
                        writeSuccessMessage($"Updated the chat inactive opacity to {opacity}.");
                        Settings.InactiveChatOpacity = opacity;
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseNumber, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetDefaultChatChannel, Settings.SetDefaultChatChannelCommandName, (text, instance) =>
                {
                    var type = new CombinedMessageType(CustomMessageType.Global);
                    bool success = false;

                    if (Enum.TryParse(text, true, out Talker.Type talkerType)
                        && talkerType != Talker.Type.Ping)
                    {
                        type.Set(talkerType);
                        success = true;
                    }
                    else
                    {
                        if (Enum.TryParse(text, true, out CustomMessageType customType))
                        {
                            type.Set(customType);
                            success = true;
                        }
                    }

                    if (success)
                    {
                        writeSuccessMessage($"Updated the default chat channel to {text}.");
                        Settings.DefaultChatChannel = type;
                    }
                    else
                    {
                        writeErrorMessage($"Failed to convert \"{text}\" into a chat channel name. Accepted values: {string.Join(", ", Enum.GetNames(typeof(Talker.Type)).Except(new[] { nameof(Talker.Type.Ping) }).Concat(Enum.GetNames(typeof(CustomMessageType))).Distinct().Select(x => x.ToLower()))}.");
                    }
                }),
                new PluginCommand(PluginCommandType.SetChatWidth, Settings.SetWidthCommandName, (text, instance) =>
                {
                    // Set to default if no argument is provided
                    if (string.IsNullOrEmpty(text))
                    {
                        text = "400";
                    }

                    if (uint.TryParse(text, out uint value))
                    {
                        value = Math.Max(200, Math.Min(1920, value));
                        writeSuccessMessage($"Updated the chat width to {value}.");

                        var chatWindow = ((Chat)instance)?.m_chatWindow;
                        if (chatWindow != null)
                        {
                            chatWindow.sizeDelta = new Vector2(value, chatWindow.sizeDelta.y);
                        }
                        Settings.ChatWidth = value;
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseNumber, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetChatHeight, Settings.SetHeightCommandName, (text, instance) =>
                {
                    // Set to default if no argument is provided
                    if (string.IsNullOrEmpty(text))
                    {
                        text = "400";
                    }

                    if (uint.TryParse(text, out uint value))
                    {
                        value = Math.Max(200, Math.Min(1080, value));
                        writeSuccessMessage($"Updated the chat height to {value}.");

                        var chatWindow = ((Chat)instance)?.m_chatWindow;
                        if (chatWindow != null)
                        {
                            chatWindow.sizeDelta = new Vector2(chatWindow.sizeDelta.x, value);
                        }
                        Settings.ChatHeight = value;
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseNumber, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetChatBufferSize, Settings.SetBufferSizeCommandName, (text, instance) =>
                {
                    // Set to default if no argument is provided
                    if (string.IsNullOrEmpty(text))
                    {
                        text = "15";
                    }

                    if (uint.TryParse(text, out uint value))
                    {
                        value = Math.Max(15, Math.Min(1000, value));
                        writeSuccessMessage($"Updated the chat buffer size to {value}.");

                        var chatWindow = ((Chat)instance)?.m_chatWindow;
                        Settings.ChatBufferSize = value;
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseNumber, text));
                    }
                })
            );
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

        public static long GetServerPeerId()
        {
            if (IsPlayerHostedServer)
            {
                return ZRoutedRpc.instance.GetServerPeerID();
            }
            else
            {
                return ZNet.instance.GetServerPeer()?.m_uid ?? long.MaxValue;
            }
        }
    }
}
