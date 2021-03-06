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
                    color = Color.white;
                    break;
                case Talker.Type.Shout:
                    color = Color.yellow;
                    break;
                case Talker.Type.Whisper:
                    color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
                    break;
            }
            return color;
        }

        public static string GetFormattedMessage(Talker.Type type, string user, string text)
        {
            var color = GetTextColor(type);
            return $"<color=orange>{user}</color>: <color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }
    }
}
