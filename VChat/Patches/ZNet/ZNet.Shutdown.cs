using HarmonyLib;
using UnityEngine;
using VChat.Messages;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
    public static class ZNetPatchShutdown
    {
        public static void Prefix(ref ZNet __instance)
        {
            // Reset the server greeting variables when the client disconnects.
            // These will be set once the client connects to a server.
            if (!ZNet.m_isServer)
            {
                VChatPlugin.Log("Resetting variables for local player");
                GreetingMessage.ResetClientVariables();
            }
        }
    }
}
