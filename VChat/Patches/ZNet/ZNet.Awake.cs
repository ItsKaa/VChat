using HarmonyLib;
using VChat.Messages;
using VChat.Services;

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
            ChannelCreateMessage.Register();
            ChannelInviteMessage.Register();
            ChannelEditMessage.Register();
            ChannelChatMessage.Register();
            ChannelDisbandMessage.Register();
            ChannelLeaveMessage.Register();
            ChannelKickMessage.Register();

            // Read stored data
            PluginDataManager.Read();

            // Initialise server commands
            VChatPlugin.InitialiseServerCommands();
        }
    }
}