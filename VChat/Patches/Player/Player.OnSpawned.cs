using HarmonyLib;
using UnityEngine;
using VChat.Messages;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class PlayerPatchOnSpawned
    {
        private static bool HasSentPluginMessage { get; set; } = false;

        public static void Prefix(ref Player __instance)
        {
            // This is only for clients. Add a chat message to the chat box displaying the status of VChat.
            if (!HasSentPluginMessage)
            {
                var chat = Chat.instance;
                Debug.LogError($"attempting to send VChat greeting message ({GreetingMessage.HasLocalPlayerGreetedToServer}, {GreetingMessage.HasLocalPlayerReceivedGreetingFromServer})");
                if (chat != null)
                {
                    if (GreetingMessage.HasLocalPlayerGreetedToServer && GreetingMessage.HasLocalPlayerReceivedGreetingFromServer)
                    {
                        chat.AddString($"<color=lime>[{VChatPlugin.Name}] Connected to the server-wide instance of {VChatPlugin.Name}, server version of {GreetingMessage.ServerVersion}.</color>");
                    }
                    else if (GreetingMessage.HasLocalPlayerGreetedToServer && !GreetingMessage.HasLocalPlayerReceivedGreetingFromServer)
                    {
                        chat.AddString($"<color=white>[{VChatPlugin.Name}] was not found on the server, custom channels like global chat will not work.</color>");
                    }
                    else
                    {
                        chat.AddString($"<color=red>[{VChatPlugin.Name}] Has encountered an issue greeting the server.</color>");
                    }
                }

                HasSentPluginMessage = true;
            }
        }
    }
}
