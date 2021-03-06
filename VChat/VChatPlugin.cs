﻿using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        public static Talker.Type CurrentChatType { get; set; }

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
            var userColor = Color.Lerp(textColor, Color.black, 0.33f);
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(userColor)}>{user}</color>: <color=#{ColorUtility.ToHtmlStringRGBA(textColor)}>{text}</color>";
        }

        public static bool UpdateCurrentChatTypeAndColor(InputField inputField, string text)
        {
            if (inputField != null && text != null)
            {
                Talker.Type chatType;
                if (text.StartsWith("/s ", StringComparison.CurrentCultureIgnoreCase))
                {
                    chatType = Talker.Type.Shout;
                }
                else if (text.StartsWith("/w ", StringComparison.CurrentCultureIgnoreCase))
                {
                    chatType = Talker.Type.Whisper;
                }
                else
                {
                    chatType = (Settings.AutoShout ? Talker.Type.Shout : Talker.Type.Normal);
                }

                if (CurrentChatType != chatType)
                {
                    CurrentChatType = chatType;
                    UpdateChatInputColor(inputField, CurrentChatType);
                    return true;
                }
            }

            return false;
        }

        public static void UpdateChatInputColor(InputField inputField, Talker.Type type)
        {
            if (inputField?.textComponent != null)
            {
                inputField.textComponent.color = GetTextColor(type);
            }
        }

    }
}
