using BepInEx.Configuration;
using System;
using UnityEngine;
using VChat.Extensions;

namespace VChat.Configuration
{
    public class PluginSettings : IDisposable
    {
        private const string ColorSection = "Colors";
        private const string ChatWindowSection = "ChatWindow";
        private const string ColorDescription = "Sets the chat colors, colors can either be a html string, such as \"#e01414\", or one of the following color names: white, black, grey, gray, red, green, blue, yellow, cyan, magenta.";
        private const string ChatWindowDescription = "Change options for the chat window, should be self explanatory.";

        public ConfigFile ConfigFile { get; private set; }

        #region Colors
        private ConfigEntry<string> LocalChatColorEntry { get; set; }
        public Color? LocalChatColor
        {
            get => LocalChatColorEntry.Value.GetColorFromString();
            set => LocalChatColorEntry.Value = value == null ? null : $"#{ColorUtility.ToHtmlStringRGBA(value.Value)}";
        }

        private ConfigEntry<string> ShoutChatColorEntry { get; set; }
        public Color? ShoutChatColor
        {
            get => ShoutChatColorEntry.Value.GetColorFromString();
            set => ShoutChatColorEntry.Value = value == null ? null : $"#{ColorUtility.ToHtmlStringRGBA(value.Value)}";
        }

        private ConfigEntry<string> WhisperChatColorEntry { get; set; }
        public Color? WhisperChatColor
        {
            get => WhisperChatColorEntry.Value.GetColorFromString();
            set => WhisperChatColorEntry.Value = value == null ? null : $"#{ColorUtility.ToHtmlStringRGBA(value.Value)}";
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

        private ConfigEntry<bool> AutoShoutEntry { get; set; }
        public bool AutoShout
        {
            get => AutoShoutEntry.Value;
            set => AutoShoutEntry.Value = value;
        }
        
        private ConfigEntry<ushort> MaxPlayerMessageHistoryCountEntry { get; set; }
        public ushort MaxPlayerMessageHistoryCount
        {
            get => MaxPlayerMessageHistoryCountEntry.Value;
            set => MaxPlayerMessageHistoryCountEntry.Value = value;
        }

        #endregion Chat Window
        
        public PluginSettings(ConfigFile configFile)
        {
            ConfigFile = configFile;
            SetupConfig();
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

            // Chat window
            AlwaysShowChatWindowEntry = ConfigFile.Bind(ChatWindowSection, nameof(AlwaysShowChatWindow), false, ChatWindowDescription);
            ShowChatWindowOnMessageReceivedEntry = ConfigFile.Bind(ChatWindowSection, nameof(ShowChatWindowOnMessageReceived), true, string.Empty);
            EnableClickThroughChatWindowEntry = ConfigFile.Bind(ChatWindowSection, nameof(EnableClickThroughChatWindow), true, string.Empty);
            AutoShoutEntry = ConfigFile.Bind(ChatWindowSection, nameof(AutoShout), true, string.Empty);
            MaxPlayerMessageHistoryCountEntry = ConfigFile.Bind(ChatWindowSection, nameof(MaxPlayerMessageHistoryCount), (ushort)25u, string.Empty);

            // Create the config file
            ConfigFile.Save();
        }
    }
}
