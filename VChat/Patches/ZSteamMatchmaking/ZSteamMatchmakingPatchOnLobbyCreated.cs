using HarmonyLib;
using Steamworks;
using VChat.Messages;

namespace VChat.Patches
{

    [HarmonyPatch(typeof(ZSteamMatchmaking), nameof(ZSteamMatchmaking.OnLobbyCreated))]
    public static class ZSteamMatchmakingPatchOnLobbyCreated
    {
        private static void Postfix(ref ZSteamMatchmaking __instance, ref LobbyCreated_t data, ref bool ioError)
        {
            if (data.m_eResult == EResult.k_EResultOK && ZNet.instance?.IsDedicated() == false)
            {
                VChatPlugin.Log($"Registered player-hosted lobby with SteamID {data.m_ulSteamIDLobby}");
                VChatPlugin.IsPlayerHostedServer = true;
            }
            else
            {
                VChatPlugin.LogWarning($"Player-hosted lobby error occurred {data.m_eResult}, resetting variables.");
                VChatPlugin.IsPlayerHostedServer = false;
                GreetingMessage.Reset();
                VChatPlugin.InitialiseServerCommands();
            }
        }
    }
}
