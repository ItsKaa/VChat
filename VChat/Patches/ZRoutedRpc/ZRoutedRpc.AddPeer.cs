using HarmonyLib;
using VChat.Messages;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.AddPeer))]
    public static class ZRoutedRpcPatchAddPeer
    {
        public static void Prefix(ref ZRoutedRpc __instance, ref ZNetPeer peer)
        {
            // Send a greeting message to the server or client.
            if (ZNet.m_isServer)
            {
                GreetingMessage.SendToClient(peer.m_uid);
            }
            else
            {
                GreetingMessage.SendToServer();
            }
        }
    }
}
