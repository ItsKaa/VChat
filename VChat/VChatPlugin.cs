using BepInEx;
using HarmonyLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using VChat.Configuration;
using VChat.Data;

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

        static VChatPlugin()
        {
            ReceivedMessageInfo = new ConcurrentDictionary<long, UserMessageInfo>();
            MessageSendHistory = new List<string>();
        }

        public void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
            Settings = new PluginSettings(Config);
        }

        public static Color GetTextColor(Talker.Type type)
        {
            var color = Color.white;
            switch (type)
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
            return color;
        }

        public static string GetFormattedMessage(Talker.Type type, string user, string text)
        {
            var textColor = GetTextColor(type);
            var userColor = new Color(textColor.r, textColor.g, textColor.b, 0.33f);
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(userColor)}>{user}</color>: <color=#{ColorUtility.ToHtmlStringRGBA(textColor)}>{text}</color>";
        }
    }
}
