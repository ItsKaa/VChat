using HarmonyLib;
using VChat.Messages;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNetPatchAwake
    {
        private static void Postfix(ref ZNet __instance)
        {
            // Register our custom defined messages.
            GlobalMessages.Register();
            GreetingMessage.Register();
            ChannelInfoMessage.Register();

            // Initialise server commands
            VChatPlugin.InitialiseServerCommands();
        }
    }
}