using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VChat.Extensions;

namespace VChat.Configuration
{
    public class PluginSettings : IDisposable
    {
        private const string ListSeparator = "|";
        private const string DefaultCommandPrefix = "/";
        private const string ColorSection = "Colors";
        private const string ChatWindowSection = "ChatWindow";
        private const string CommandsSection = "Commands";
        private const string ColorDescription = "Sets the chat colors, colors can either be a html string, such as \"#e01414\"\nor use of the following color names: white, black, grey, gray, red, green, blue, yellow, cyan, magenta.";
        private const string ChatWindowDescription = "Change options for the chat window, should be self explanatory.";
        private const string CommandDescription = "The command prefix determines what string is used to start commands, this can be anything you like.\nTo set aliases for command names, use the separator '|', e.g.: \"one|two|three\", meaning /three will execute the same command as /one, assuming the command prefix is set to the default.\nPlease do not enter the prefix in the command names.";

        public ConfigFile ConfigFile { get; private set; }
        private DateTime LastTickDate { get; set; }
        private DateTime LastFileModifiedDate { get; set; }

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

        private ConfigEntry<bool> ShowChatWindowOnMessageReceivedEntry { get; set; }
        public bool ShowChatWindowOnMessageReceived
        {
            get => ShowChatWindowOnMessageReceivedEntry.Value;
            set => ShowChatWindowOnMessageReceivedEntry.Value = value;
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

            // Colors
            LocalChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(LocalChatColor), null, ColorDescription);
            ShoutChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(ShoutChatColor), null, string.Empty);
            WhisperChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(WhisperChatColor), null, string.Empty);
            GlobalChatColorEntry = ConfigFile.Bind<string>(ColorSection, nameof(GlobalChatColor), null, string.Empty);

            // Chat window
            AlwaysShowChatWindowEntry = ConfigFile.Bind(ChatWindowSection, nameof(AlwaysShowChatWindow), false, ChatWindowDescription);
            ShowChatWindowOnMessageReceivedEntry = ConfigFile.Bind(ChatWindowSection, nameof(ShowChatWindowOnMessageReceived), true, string.Empty);
            EnableClickThroughChatWindowEntry = ConfigFile.Bind(ChatWindowSection, nameof(EnableClickThroughChatWindow), true, string.Empty);
            MaxPlayerMessageHistoryCountEntry = ConfigFile.Bind(ChatWindowSection, nameof(MaxPlayerMessageHistoryCount), (ushort)25u, string.Empty);

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

            // Create the config file
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
