using HarmonyLib;
using VChat.Data;
using VChat.Helpers;
using VChat.Messages;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.InputText))]
    public static class ChatPatchInputText
    {
        private static bool Prefix(ref Chat __instance)
        {
            var text = __instance.m_input.text;

            // Add the message to the chat history.
            if (VChatPlugin.Settings.MaxPlayerMessageHistoryCount > 0)
            {
                if (VChatPlugin.MessageSendHistory.Count > VChatPlugin.Settings.MaxPlayerMessageHistoryCount)
                {
                    VChatPlugin.MessageSendHistory.RemoveAt(0);
                }

                VChatPlugin.MessageSendHistory.Add(text);
                VChatPlugin.MessageSendHistoryIndex = 0;
            }

            // Parse client or server commands.
            if (VChatPlugin.CommandHandler.TryFindAndExecuteClientCommand(text, __instance, out PluginCommandClient _))
            {
                return false;
            }
            else if (VChatPlugin.CommandHandler.TryFindAndExecuteServerCommand(text, ValheimHelper.GetLocalPlayerPeerId(), ValheimHelper.GetLocalPlayerSteamId(), out PluginCommandServer _))
            {
                return false;
            }

            // Otherwise send the message to the last used channel.
            // Only when not starting with a slash, because that's the default for commands. We still want /sit and such to work :)
            if (!text.StartsWith("/"))
            {
                if (VChatPlugin.LastChatType.IsDefaultType())
                {
                    __instance.SendText(VChatPlugin.LastChatType.DefaultTypeValue.Value, text);
                    return false;
                }
                else
                {
                    switch (VChatPlugin.LastChatType.CustomTypeValue)
                    {
                        case CustomMessageType.Global:
                            GlobalMessages.SendGlobalMessageToServer(text);
                            break;
                    }
                    return false;
                }
            }

            return true;
        }
    }
}
