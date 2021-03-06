using BepInEx;
using HarmonyLib;
using System.Collections.Concurrent;
using UnityEngine;
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

        public static ConcurrentDictionary<long, UserMessageInfo> ReceivedMessageInfo { get; set; }

        public static Color LocalChatColor { get; set; } = Color.white;
        public static Color ShoutChatColor { get; set; } = Color.yellow;
        public static Color WhisperChatColor { get; set; } = new Color(1.0f, 1.0f, 1.0f, 0.75f);
        public static bool AutoShout { get; set; } = false;

        static VChatPlugin()
        {
            ReceivedMessageInfo = new ConcurrentDictionary<long, UserMessageInfo>();
        }

        public void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        public static Color GetTextColor(Talker.Type type)
        {
            var color = Color.white;
            switch (type)
            {
                case Talker.Type.Normal:
                    color = LocalChatColor;
                    break;
                case Talker.Type.Shout:
                    color = ShoutChatColor;
                    break;
                case Talker.Type.Whisper:
                    color = WhisperChatColor;
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
