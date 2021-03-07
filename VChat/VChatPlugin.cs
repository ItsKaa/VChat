using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VChat.Configuration;
using VChat.Data;
using VChat.Extensions;
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
        public const string Version = "0.1.0";
        public const string Repository = "https://github.com/ItsKaa/VChat";

        internal static PluginSettings Settings { get; private set; }
        public static ConcurrentDictionary<long, UserMessageInfo> ReceivedMessageInfo { get; set; }
        public static List<string> MessageSendHistory { get; private set; }
        public static int MessageSendHistoryIndex { get; set; } = 0;
        public static CombinedMessageType CurrentChatType { get; set; }
        public static CommandHandler CommandHandler { get; set; }

        static VChatPlugin()
        {
            ReceivedMessageInfo = new ConcurrentDictionary<long, UserMessageInfo>();
            MessageSendHistory = new List<string>();
            CommandHandler = new CommandHandler();
            CurrentChatType = new CombinedMessageType(Talker.Type.Normal);
        }

        public void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
            Settings = new PluginSettings(Config);
            InitialiseCommands();

            Debug.Log($"{Name} initialised, version {Version}.");
        }

        private void InitialiseCommands()
        {
            CommandHandler.ClearCommands();

            var writeErrorMessage = new Action<string>((string message) =>
            {
                Chat.instance.AddString($"<color=red>[Chat][Error] {message}</color>");
            });

            var writeSuccessMessage = new Action<string>((string message) =>
            {
                Chat.instance.AddString($"<color=#23ff00>[Chat] {message}</color>");
            });

            const string changedColorMessageSuccess = "Changed the {0} color to <color={1}>color</color>.";
            const string errorParseColorMessage = "Could not parse the color \"{0}\".";

            CommandHandler.AddCommands(
                new PluginCommand(PluginCommandType.SendLocalMessage, Settings.LocalChatCommandName, (text, instance) =>
                {
                    ((Chat)instance).SendText(Talker.Type.Normal, text);
                }),
                new PluginCommand(PluginCommandType.SendShoutMessage, Settings.ShoutChatCommandName, (text, instance) =>
                {
                    ((Chat)instance).SendText(Talker.Type.Shout, text);
                }),
                new PluginCommand(PluginCommandType.SendWhisperMessage, Settings.WhisperChatCommandName, (text, instance) =>
                {
                    ((Chat)instance).SendText(Talker.Type.Whisper, text);
                }),
                new PluginCommand(PluginCommandType.SendGlobalMessage, Settings.GlobalChatCommandName, (text, instance) =>
                {
                    GlobalMessages.SendGlobalMessageToServer(text);
                }),
                new PluginCommand(PluginCommandType.SetLocalColor, Settings.SetLocalChatColorCommandName, (text, instance) =>
                {
                    text = text?.Trim();
                    var color = text?.ToColor();
                    if (color != null)
                    {
                        Settings.LocalChatColor = color;
                        writeSuccessMessage(string.Format(changedColorMessageSuccess, "local", color?.ToHtmlString()));
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseColorMessage, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetShoutColor, Settings.SetShoutChatColorCommandName, (text, instance) =>
                {
                    text = text?.Trim();
                    var color = text?.ToColor();
                    if (color != null)
                    {
                        Settings.ShoutChatColor = color;
                        writeSuccessMessage(string.Format(changedColorMessageSuccess, "shout", color?.ToHtmlString()));
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseColorMessage, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetWhisperColor, Settings.SetWhisperChatColorCommandName, (text, instance) =>
                {
                    text = text?.Trim();
                    var color = text?.ToColor();
                    if (color != null)
                    {
                        Settings.WhisperChatColor = color;
                        writeSuccessMessage(string.Format(changedColorMessageSuccess, "whisper", color?.ToHtmlString()));
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseColorMessage, text));
                    }
                }),
                new PluginCommand(PluginCommandType.SetGlobalColor, Settings.GlobalWhisperChatColorCommandName, (text, instance) =>
                {
                    text = text?.Trim();
                    var color = text?.ToColor();
                    if (color != null)
                    {
                        Settings.GlobalChatColor = color;
                        writeSuccessMessage(string.Format(changedColorMessageSuccess, "global", color?.ToHtmlString()));
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseColorMessage, text));
                    }
                }),
                new PluginCommand(PluginCommandType.ToggleAutoShout, Settings.AutoShoutCommandName, (text, instance) =>
                {
                    Settings.AutoShout = !Settings.AutoShout;
                    writeSuccessMessage($"{(Settings.AutoShout ? "Enabled" : "Disabled")} auto shout.");
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
                            writeErrorMessage($"Could not convert the value \"{text}\" to a number.");
                        }
                    }
                })
            );
        }

        public static Color GetTextColor(CombinedMessageType type)
        {
            var color = Color.white;
            if (type.IsDefaultType())
            {
                switch (type.DefaultTypeValue.Value)
                {
                    case Talker.Type.Normal:
                        color = Settings.LocalChatColor ?? Color.white;
                        break;
                    case Talker.Type.Shout:
                        color = Settings.ShoutChatColor ?? Color.yellow;
                        break;
                    case Talker.Type.Whisper:
                        color = Settings.WhisperChatColor ?? new Color(1.0f, 1.0f, 1.0f, 0.75f);
                        break;
                }
            }
            else if(type.IsCustomType())
            {
                switch(type.CustomTypeValue.Value)
                {
                    case CustomMessageType.GlobalChat:
                        color = Settings.GlobalChatColor ?? new Color(0.890f, 0.376f, 0.050f);
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
                var messageType = new CombinedMessageType(Settings.AutoShout ? Talker.Type.Shout : Talker.Type.Normal);

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
                        messageType.Set(CustomMessageType.GlobalChat);
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
                if (!CurrentChatType.Equals(messageType))
                {
                    CurrentChatType = messageType;
                    UpdateChatInputColor(inputField, CurrentChatType);
                    return true;
                }
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

    }
}
