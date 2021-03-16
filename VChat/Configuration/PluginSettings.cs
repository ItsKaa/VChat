using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VChat.Data;
using VChat.Extensions;

namespace VChat.Configuration
{
    public class PluginSettings : IDisposable
    {
        private const string ListSeparator = "|";
        private const string DefaultCommandPrefix = "/";
        private const string GeneralSection = "General";
        private const string ColorSection = "Colors";
        private const string ChatWindowSection = "ChatWindow";
        private const string CommandsSection = "Commands";
        private const string ServerSection = "Server";
        private const string ColorDescription = "Sets the chat colors, colors can either be a html string, such as \"#e01414\"\nor use of the following color names: white, black, grey, gray, red, green, blue, yellow, cyan, magenta.";
        private const string ChatWindowDescription = "Change options for the chat window, should be self explanatory.";
        private const string CommandDescription = "The command prefix determines what string is used to start commands, this can be anything you like.\nTo set aliases for command names, use the separator '|', e.g.: \"one|two|three\", meaning /three will execute the same command as /one, assuming the command prefix is set to the default.\nPlease do not enter the prefix in the command names.";
        private const string ServerDescription = "Settings for when hosting a server, should be self explanatory. Please note that some settings may be limited to dedicated servers.";

        public ConfigFile ConfigFile { get; private set; }
        private DateTime LastTickDate { get; set; }
        private DateTime LastFileModifiedDate { get; set; }

        #region General
        private ConfigEntry<string> VersionEntry { get; set; }
        public string Version
        {
            get => VersionEntry.Value;
            set => VersionEntry.Value = value;
        }

        #endregion General
        #region Colors
        private ConfigEntry<string> LocalChatColorEntry { get; set; }
        public Color? LocalChatColor
        {
            get => LocalChatColorEntry.Value?.ToColor();
            set => LocalChatColorEntry.Value = value?.ToHtmlString() ?? null;
        }

        private ConfigEntry<string> ShoutChatColorEntry { get; set; }
        public Color? ShoutChatColor
        {
            get => ShoutChatColorEntry.Value.ToColor();
            set => ShoutChatColorEntry.Value = value?.ToHtmlString() ?? null;
        }

        private ConfigEntry<string> WhisperChatColorEntry { get; set; }
        public Color? WhisperChatColor
        {
            get => WhisperChatColorEntry.Value.ToColor();
            set => WhisperChatColorEntry.Value = value?.ToHtmlString() ?? null;
        }

        private ConfigEntry<string> GlobalChatColorEntry { get; set; }
        public Color? GlobalChatColor
        {
            get => GlobalChatColorEntry.Value.ToColor();
            set => GlobalChatColorEntry.Value = value?.ToHtmlString() ?? null;
        }
        #endregion Colors
        #region Chat Window
        private ConfigEntry<bool> AlwaysShowChatWindowEntry { get; set; }
        public bool AlwaysShowChatWindow
        {
            get => AlwaysShowChatWindowEntry.Value;
            set => AlwaysShowChatWindowEntry.Value = value;
        }

        private ConfigEntry<string> DefaultChatChannelEntry { get; set; }
        public CombinedMessageType DefaultChatChannel
        {
            get
            {
                var type = new CombinedMessageType(CustomMessageType.Global);
                if (Enum.TryParse(DefaultChatChannelEntry.Value, true, out Talker.Type talkerType)
                    && talkerType != Talker.Type.Ping)
                {
                    type.Set(talkerType);
                }
                else
                {
                    if (Enum.TryParse(DefaultChatChannelEntry.Value, true, out CustomMessageType customType))
                    {
                        type.Set(customType);
                    }
                    else
                    {
                        VChatPlugin.LogWarning($"Failed to convert {DefaultChatChannelEntry.Value} to an enum.");
                    }
                }
                return type;
            }
            set => DefaultChatChannelEntry.Value = value.ToString();
        }

        private ConfigEntry<bool> ShowChatWindowOnMessageReceivedEntry { get; set; }
        public bool ShowChatWindowOnMessageReceived
        {
            get => ShowChatWindowOnMessageReceivedEntry.Value;
            set => ShowChatWindowOnMessageReceivedEntry.Value = value;
        }
        private ConfigEntry<bool> UseChatOpacityEntry { get; set; }
        public bool UseChatOpacity
        {
            get => UseChatOpacityEntry.Value;
            set => UseChatOpacityEntry.Value = value;
        }
        
        private ConfigEntry<uint> ChatOpacityEntry { get; set; }
        public uint ChatOpacity
        {
            get => ChatOpacityEntry.Value;
            set => ChatOpacityEntry.Value = value;
        }

        private ConfigEntry<uint> InactiveChatOpacityEntry { get; set; }
        public uint InactiveChatOpacity
        {
            get => InactiveChatOpacityEntry.Value;
            set => InactiveChatOpacityEntry.Value = value;
        }

        private ConfigEntry<float> ChatHideDelayEntry { get; set; }
        public float ChatHideDelay
        {
            get => ChatHideDelayEntry.Value;
            set => ChatHideDelayEntry.Value = value;
        }

        private ConfigEntry<float> ChatFadeTimerEntry { get; set; }
        public float ChatFadeTimer
        {
            get => ChatFadeTimerEntry.Value;
            set => ChatFadeTimerEntry.Value = value;
        }

        private ConfigEntry<bool> EnableClickThroughChatWindowEntry { get; set; }
        public bool EnableClickThroughChatWindow
        {
            get => EnableClickThroughChatWindowEntry.Value;
            set => EnableClickThroughChatWindowEntry.Value = value;
        }

        private ConfigEntry<ushort> MaxPlayerMessageHistoryCountEntry { get; set; }
        public ushort MaxPlayerMessageHistoryCount
        {
            get => MaxPlayerMessageHistoryCountEntry.Value;
            set => MaxPlayerMessageHistoryCountEntry.Value = value;
        }

        private ConfigEntry<uint> ChatWidthEntry { get; set; }
        public uint ChatWidth
        {
            get => Math.Max(200, Math.Min(1920, ChatWidthEntry.Value));
            set => ChatWidthEntry.Value = Math.Max(200, Math.Min(1920, value));
        }

        private ConfigEntry<uint> ChatHeightEntry { get; set; }
        public uint ChatHeight
        {
            get => Math.Max(200, Math.Min(1080, ChatHeightEntry.Value));
            set => ChatHeightEntry.Value = Math.Max(200, Math.Min(1080, value));
        }

        private ConfigEntry<uint> ChatBufferSizeEntry { get; set; }
        public uint ChatBufferSize
        {
            get => Math.Max(15, Math.Min(1000, VChatPlugin.Settings.ChatBufferSizeEntry.Value));
            set => ChatBufferSizeEntry.Value = Math.Max(15, Math.Min(1000, value));
        }

        #endregion Chat Window
        #region Commands
        private ConfigEntry<string> CommandPrefixEntry { get; set; }
        public string CommandPrefix
        {
            get
            {
                var value = CommandPrefixEntry.Value?.Trim();
                return string.IsNullOrEmpty(value) ? DefaultCommandPrefix : value;
            }
            set => CommandPrefixEntry.Value = value;
        }

        #region Command Names
        #region Command Names: Chat Window
        private ConfigEntry<string> ShowChatCommandNameEntry { get; set; }
        public IEnumerable<string> ShowChatCommandName
        {
            get => GetConfigEntryAsCollection(ShowChatCommandNameEntry);
            set => SetConfigEntryValue(ShowChatCommandNameEntry, value);
        }

        private ConfigEntry<string> ShowChatOnMessageCommandNameEntry { get; set; }
        public IEnumerable<string> ShowChatOnMessageCommandName
        {
            get => GetConfigEntryAsCollection(ShowChatOnMessageCommandNameEntry);
            set => SetConfigEntryValue(ShowChatOnMessageCommandNameEntry, value);
        }

        private ConfigEntry<string> ChatClickThroughCommandNameEntry { get; set; }
        public IEnumerable<string> ChatClickThroughCommandName
        {
            get => GetConfigEntryAsCollection(ChatClickThroughCommandNameEntry);
            set => SetConfigEntryValue(ChatClickThroughCommandNameEntry, value);
        }

        private ConfigEntry<string> MaxPlayerChatHistoryCommandNameEntry { get; set; }
        public IEnumerable<string> MaxPlayerChatHistoryCommandName
        {
            get => GetConfigEntryAsCollection(MaxPlayerChatHistoryCommandNameEntry);
            set => SetConfigEntryValue(MaxPlayerChatHistoryCommandNameEntry, value);
        }

        private ConfigEntry<string> SetChatHideDelayCommandNameEntry { get; set; }
        public IEnumerable<string> SetChatHideDelayCommandName
        {
            get => GetConfigEntryAsCollection(SetChatHideDelayCommandNameEntry);
            set => SetConfigEntryValue(SetChatHideDelayCommandNameEntry, value);
        }

        private ConfigEntry<string> SetChatFadeTimeCommandNameEntry { get; set; }
        public IEnumerable<string> SetChatFadeTimeCommandName
        {
            get => GetConfigEntryAsCollection(SetChatFadeTimeCommandNameEntry);
            set => SetConfigEntryValue(SetChatFadeTimeCommandNameEntry, value);
        }

        private ConfigEntry<string> SetOpacityCommandNameEntry { get; set; }
        public IEnumerable<string> SetOpacityCommandName
        {
            get => GetConfigEntryAsCollection(SetOpacityCommandNameEntry);
            set => SetConfigEntryValue(SetOpacityCommandNameEntry, value);
        }

        private ConfigEntry<string> SetInactiveOpacityCommandNameEntry { get; set; }
        public IEnumerable<string> SetInactiveOpacityCommandName
        {
            get => GetConfigEntryAsCollection(SetInactiveOpacityCommandNameEntry);
            set => SetConfigEntryValue(SetInactiveOpacityCommandNameEntry, value);
        }

        private ConfigEntry<string> SetDefaultChatChannelCommandNameEntry { get; set; }
        public IEnumerable<string> SetDefaultChatChannelCommandName
        {
            get => GetConfigEntryAsCollection(SetDefaultChatChannelCommandNameEntry);
            set => SetConfigEntryValue(SetDefaultChatChannelCommandNameEntry, value);
        }

        private ConfigEntry<string> SetWidthCommandNameEntry { get; set; }
        public IEnumerable<string> SetWidthCommandName
        {
            get => GetConfigEntryAsCollection(SetWidthCommandNameEntry);
            set => SetConfigEntryValue(SetWidthCommandNameEntry, value);
        }

        private ConfigEntry<string> SetBufferSizeCommandNameEntry { get; set; }
        public IEnumerable<string> SetBufferSizeCommandName
        {
            get => GetConfigEntryAsCollection(SetBufferSizeCommandNameEntry);
            set => SetConfigEntryValue(SetBufferSizeCommandNameEntry, value);
        }

        private ConfigEntry<string> SetHeightCommandNameEntry { get; set; }
        public IEnumerable<string> SetHeightCommandName
        {
            get => GetConfigEntryAsCollection(SetHeightCommandNameEntry);
            set => SetConfigEntryValue(SetHeightCommandNameEntry, value);
        }

        #endregion Command Names: Chat Window
        #region Command Names: Channels
        private ConfigEntry<string> LocalChatCommandNameEntry { get; set; }
        public IEnumerable<string> LocalChatCommandName
        {
            get => GetConfigEntryAsCollection(LocalChatCommandNameEntry);
            set => SetConfigEntryValue(LocalChatCommandNameEntry, value);
        }

        private ConfigEntry<string> ShoutChatCommandNameEntry { get; set; }
        public IEnumerable<string> ShoutChatCommandName
        {
            get => GetConfigEntryAsCollection(ShoutChatCommandNameEntry);
            set => SetConfigEntryValue(ShoutChatCommandNameEntry, value);
        }

        private ConfigEntry<string> WhisperChatCommandNameEntry { get; set; }
        public IEnumerable<string> WhisperChatCommandName
        {
            get => GetConfigEntryAsCollection(WhisperChatCommandNameEntry);
            set => SetConfigEntryValue(WhisperChatCommandNameEntry, value);
        }

        private ConfigEntry<string> GlobalChatCommandNameEntry { get; set; }
        public IEnumerable<string> GlobalChatCommandName
        {
            get => GetConfigEntryAsCollection(GlobalChatCommandNameEntry);
            set => SetConfigEntryValue(GlobalChatCommandNameEntry, value);
        }
        #endregion Command Names: Channels
        #region Command Names: Colors
        private ConfigEntry<string> SetLocalChatColorCommandNameEntry { get; set; }
        public IEnumerable<string> SetLocalChatColorCommandName
        {
            get => GetConfigEntryAsCollection(SetLocalChatColorCommandNameEntry);
            set => SetConfigEntryValue(SetLocalChatColorCommandNameEntry, value);
        }

        private ConfigEntry<string> SetShoutChatColorCommandNameEntry { get; set; }
        public IEnumerable<string> SetShoutChatColorCommandName
        {
            get => GetConfigEntryAsCollection(SetShoutChatColorCommandNameEntry);
            set => SetConfigEntryValue(SetShoutChatColorCommandNameEntry, value);
        }

        private ConfigEntry<string> SetWhisperChatColorCommandNameEntry { get; set; }
        public IEnumerable<string> SetWhisperChatColorCommandName
        {
            get => GetConfigEntryAsCollection(SetWhisperChatColorCommandNameEntry);
            set => SetConfigEntryValue(SetWhisperChatColorCommandNameEntry, value);
        }

        private ConfigEntry<string> GlobalWhisperChatColorCommandNameEntry { get; set; }
        public IEnumerable<string> GlobalWhisperChatColorCommandName
        {
            get => GetConfigEntryAsCollection(GlobalWhisperChatColorCommandNameEntry);
            set => SetConfigEntryValue(GlobalWhisperChatColorCommandNameEntry, value);
        }
        #endregion Command Names: Colors
        #endregion Command Names
        #endregion Commands
        #region Server
        private ConfigEntry<bool> EnableModCompatibilityEntry { get; set; }
        public bool EnableModCompatibility
        {
            get => EnableModCompatibilityEntry.Value;
            set => EnableModCompatibilityEntry.Value = value;
        }

        private ConfigEntry<bool> SendGlobalMessageConfirmationToNonVChatUsersEntry { get; set; }
        public bool SendGlobalMessageConfirmationToNonVChatUsers
        {
            get => SendGlobalMessageConfirmationToNonVChatUsersEntry.Value;
            set => SendGlobalMessageConfirmationToNonVChatUsersEntry.Value = value;
        }
        #endregion Server

        public PluginSettings(ConfigFile configFile)
        {
            LastTickDate = DateTime.MinValue;
            LastFileModifiedDate = DateTime.MinValue;
            ConfigFile = configFile;
            SetupConfig();
            ConfigFile.ConfigReloaded += ConfigFile_ConfigReloaded;
        }

        /// <summary>
        /// Checks and reloads the settings when the file on disk has been modified.
        /// </summary>
        public void Tick()
        {
            if ((DateTime.UtcNow - LastTickDate).TotalSeconds > 5)
            {
                // Try-catch in case file operations fail.
                try
                {
                    if (File.Exists(ConfigFile.ConfigFilePath))
                    {
                        var modifiedDate = File.GetLastWriteTimeUtc(ConfigFile.ConfigFilePath);
                        if(LastFileModifiedDate == DateTime.MinValue)
                        {
                            LastFileModifiedDate = modifiedDate;
                        }
                        else if (modifiedDate > LastFileModifiedDate)
                        {
                            ConfigFile.Reload();
                            LastFileModifiedDate = modifiedDate;
                        }
                    }
                }
                catch (Exception ex)
                {
                    VChatPlugin.LogError($"Error in Settings.Tick: {ex}");
                }

                LastTickDate = DateTime.UtcNow;
            }
        }

        private void ConfigFile_ConfigReloaded(object sender, EventArgs e)
        {
            VChatPlugin.Log($"Reloaded settings.");
        }


        public void Dispose()
        {
        }

        private void SetupConfig()
        {
            ConfigFile.SaveOnConfigSet = true;

            // General
            VersionEntry = ConfigFile.Bind<string>(GeneralSection, nameof(Version), null, "The settings file version, this will update automatically.");

            // Colors
            LocalChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(LocalChatColor), null, ColorDescription);
            ShoutChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(ShoutChatColor), null, string.Empty);
            WhisperChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(WhisperChatColor), null, string.Empty);
            GlobalChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(GlobalChatColor), null, string.Empty);

            // Chat window
            AlwaysShowChatWindowEntry = ConfigFile.Bind(ChatWindowSection, nameof(AlwaysShowChatWindow), false, ChatWindowDescription);
            ShowChatWindowOnMessageReceivedEntry = ConfigFile.Bind(ChatWindowSection, nameof(ShowChatWindowOnMessageReceived), true, string.Empty);
            UseChatOpacityEntry = ConfigFile.Bind(ChatWindowSection, nameof(UseChatOpacity), true, "Whether the chat opacity is used, when set to true, both the opacity and chat fading options become available.");
            ChatOpacityEntry = ConfigFile.Bind(ChatWindowSection, nameof(ChatOpacity), 100u, "The opactiy value for when the chat is active.\nAccepted value range is between 0 and 100, where 0 means completely transparent and 100 is opaque.");
            InactiveChatOpacityEntry = ConfigFile.Bind(ChatWindowSection, nameof(InactiveChatOpacity), 25u, "The opacity value for when the chat is inactive.\nAccepted value range is between 0 and 100, where 0 means completely transparent and 100 is opaque.");
            ChatHideDelayEntry = ConfigFile.Bind(ChatWindowSection, nameof(ChatHideDelay), 10.0f, "The amount of seconds it should take for the chat to go inactive.");
            ChatFadeTimerEntry = ConfigFile.Bind(ChatWindowSection, nameof(ChatFadeTimer), 3.0f, "The time in seconds it should take to transition the chat window's opactiy from active to inactive (or hidden).");
            EnableClickThroughChatWindowEntry = ConfigFile.Bind(ChatWindowSection, nameof(EnableClickThroughChatWindow), true, string.Empty);
            MaxPlayerMessageHistoryCountEntry = ConfigFile.Bind(ChatWindowSection, nameof(MaxPlayerMessageHistoryCount), (ushort)25u, string.Empty);
            DefaultChatChannelEntry = ConfigFile.Bind(ChatWindowSection, nameof(DefaultChatChannel), CustomMessageType.Global.ToString().ToLower(),
                $"The default chat channel that's set when your character spawns in," +
                $"accepted values: {string.Join(", ", Enum.GetNames(typeof(Talker.Type)).Except(new[] { nameof(Talker.Type.Ping) }).Concat(Enum.GetNames(typeof(CustomMessageType))).Distinct().Select(x => x.ToLower()))}."
            );
            ChatWidthEntry = ConfigFile.Bind(ChatWindowSection, nameof(ChatWidth), 500u, "Sets the width of the chat window, the maximum value is 1920.");
            ChatHeightEntry = ConfigFile.Bind(ChatWindowSection, nameof(ChatHeight), 400u, "Sets the height of the chat window, the maximum value is 1080.");
            ChatBufferSizeEntry = ConfigFile.Bind(ChatWindowSection, nameof(ChatBufferSize), 50u, "Changes the maximum amount of messages visible in the chat window. Setting this to 15 (game default) will remove the hook.");

            // Command Names
            CommandPrefixEntry = ConfigFile.Bind(CommandsSection, nameof(CommandPrefix), DefaultCommandPrefix, CommandDescription);
            LocalChatCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(LocalChatCommandName), "s|l|say|local", string.Empty);
            ShoutChatCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(ShoutChatCommandName), "y|sh|yell|shout", string.Empty);
            WhisperChatCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(WhisperChatCommandName), "w|whisper", string.Empty);
            GlobalChatCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(GlobalChatCommandName), "g|global", string.Empty);

            SetLocalChatColorCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetLocalChatColorCommandName), "setlocalcolor", string.Empty);
            SetShoutChatColorCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetShoutChatColorCommandName), "setshoutcolor", string.Empty);
            SetWhisperChatColorCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetWhisperChatColorCommandName), "setwhispercolor", string.Empty);
            GlobalWhisperChatColorCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(GlobalWhisperChatColorCommandName), "setglobalcolor", string.Empty);

            // Chat Window
            ShowChatCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(ShowChatCommandName), "showchat", string.Empty);
            ShowChatOnMessageCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(ShowChatOnMessageCommandName), "showchatonmessage", string.Empty);
            ChatClickThroughCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(ChatClickThroughCommandName), "chatclickthrough", string.Empty);
            MaxPlayerChatHistoryCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(MaxPlayerChatHistoryCommandName), "maxplayerchathistory", string.Empty);
            SetChatHideDelayCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetChatHideDelayCommandName), "sethidetime|sethidedelay|setht", string.Empty);
            SetChatFadeTimeCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetChatFadeTimeCommandName), "setfadetime|setft", string.Empty);
            SetOpacityCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetOpacityCommandName), "setopacity|set%", string.Empty);
            SetInactiveOpacityCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetInactiveOpacityCommandName), "setinactiveopacity|setiopacity|seti%", string.Empty);
            SetDefaultChatChannelCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetDefaultChatChannelCommandName), "setdefaultchannel", string.Empty);
            SetWidthCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetWidthCommandName), "setwidth", string.Empty);
            SetHeightCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetHeightCommandName), "setheight", string.Empty);
            SetBufferSizeCommandNameEntry = ConfigFile.Bind(CommandsSection, nameof(SetBufferSizeCommandName), "setbuffersize", string.Empty);

            // Server
            EnableModCompatibilityEntry = ConfigFile.Bind(ServerSection, nameof(EnableModCompatibility), true, $"{ServerDescription}\n\nEnabling this setting will redirect global messages as a local chat message if hosted as a dedicated server. This should allow other server mods to read these messages.\nThis will allow other server mods to read the global chat messages (if they capture the Chat.OnNewChatMessage method).");
            SendGlobalMessageConfirmationToNonVChatUsersEntry = ConfigFile.Bind(ServerSection, nameof(SendGlobalMessageConfirmationToNonVChatUsers), true, "Enable this option if you wish to send a confirmation global chat message to players without VChat installed, meaning if they type \"/g [text]\" they will also see a \"[Global] [text]\" message.");

            // Create the config file
            // Version was added in 1.2.1
            // 1.2.1: Update chat width to 500 if default is set to 400, which is incorrect
            if (string.IsNullOrEmpty(Version) && ChatWidth == 400u)
            {
                ChatWidth = 500u;
            }

            // Update settings to current version and save.
            Version = VChatPlugin.Version;
            ConfigFile.Save();
        }

        public void SetConfigEntryValue(ConfigEntry<string> configEntry, IEnumerable<string> collection)
        {
            configEntry.Value = string.Join(ListSeparator, collection);
        }

        public IEnumerable<string> GetConfigEntryAsCollection(ConfigEntry<string> configEntry)
        {
            return configEntry.Value.Split(new[] { ListSeparator }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
