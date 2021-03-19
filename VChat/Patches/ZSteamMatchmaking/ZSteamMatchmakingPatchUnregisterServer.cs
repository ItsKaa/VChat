using HarmonyLib;
using VChat.Messages;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZSteamMatchmaking), nameof(ZSteamMatchmaking.UnregisterServer))]
    public static class ZSteamMatchmakingPatchUnregisterServer
    {
        private static void Postfix(ref ZSteamMatchmaking __instance)
        {
            VChatPlugin.Log($"Unregistered server, resetting variables.");
            GreetingMessage.Reset();
            PlayerPatchOnSpawned.HasPlayerSpawnedOnce = false;
            VChatPlugin.IsPlayerHostedServer = false;
        }
    }
}
