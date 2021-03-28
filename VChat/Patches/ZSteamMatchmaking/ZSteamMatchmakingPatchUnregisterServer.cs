using HarmonyLib;
using VChat.Messages;
using VChat.Services;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZSteamMatchmaking), nameof(ZSteamMatchmaking.UnregisterServer))]
    public static class ZSteamMatchmakingPatchUnregisterServer
    {
        private static void Postfix(ref ZSteamMatchmaking __instance)
        {
            VChatPlugin.Log($"Unregistered server, resetting variables.");
            GreetingMessage.Reset();
            ChannelInfoMessage.Reset();
            ServerChannelManager.Reset();
            PlayerPatchOnSpawned.HasPlayerSpawnedOnce = false;
            VChatPlugin.IsPlayerHostedServer = false;
        }
    }
}
