using HarmonyLib;
using UnityEngine;
using VChat.Data;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.OnNewChatMessage))]
    public static class ChatPatchOnNewChatMessage
    {
        private static void Prefix(ref Chat __instance, ref GameObject go, ref long senderID, ref Vector3 pos, ref Talker.Type type, ref string user, ref string text)
        {
            // Add the last received message of a specific user so that we can format the floating text later.
            var userInfo = new UserMessageInfo()
            {
                GameObject = go,
                Position = pos,
                SenderID = senderID,
                Type = type,
                User = user,
                Text = text
            };
            VChatPlugin.ReceivedMessageInfo.AddOrUpdate(senderID, userInfo, (key, oldValue) => userInfo);

            // Display chat window when a new message is received.
            if (VChatPlugin.ShowChatWindowOnMessageReceived)
            {
                __instance.m_hideTimer = 0;
            }
        }
    }
}
