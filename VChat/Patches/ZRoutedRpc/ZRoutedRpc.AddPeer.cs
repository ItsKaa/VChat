using HarmonyLib;
using VChat.Messages;
using VChat.Services;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.AddPeer))]
    public static class ZRoutedRpcPatchAddPeer
    {
        public static void Postfix(ref ZRoutedRpc __instance, ref ZNetPeer peer)
        {
            // Send a greeting message to the server or client.
            if (ZNet.m_isServer)
            {
                GreetingMessage.SendToClient(peer.m_uid);
                ServerChannelManager.SendChannelInformationToClient(peer.m_uid);
            }
            else
            {
                GreetingMessage.SendToServer();
            }
        }
    }
}
