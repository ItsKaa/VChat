using BepInEx.Configuration;
using System;
using System.IO;
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
                    Debug.LogError($"Error in Settings.Tick: {ex}");
                }

                LastTickDate = DateTime.UtcNow;
            }
        }

        private void ConfigFile_ConfigReloaded(object sender, EventArgs e)
        {
            Debug.Log($"Reloaded settings.");
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
