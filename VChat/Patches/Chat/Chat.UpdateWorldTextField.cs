using System.Linq;
using HarmonyLib;
using VChat.Data;

namespace VChat.Patches
{
    [HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTextField))]
    public static class ChatPatchUpdateWorldTextField
    {
        private static void Prefix(ref Chat __instance, ref Chat.WorldTextInstance wt)
        {
            // Apply floating text and colour.
            // This item gets added in Chat.AddInworldText, but there's a lot of code to handle if I were to intercept it, Postfix on that method works too but for I feel like the update method makes sense.
            foreach (var messageInfoPair in VChatPlugin.ReceivedMessageInfo.ToList())
            {
                var messageInfo = messageInfoPair.Value;
                if (messageInfo?.Equals(wt) == true)
                {
                    wt.m_text = messageInfo.Text;
                    wt.m_textField.color = VChatPlugin.GetTextColor(new CombinedMessageType(wt.m_type));
                    // Not required but setting the floating text anyway.
                    wt.m_textField.text = messageInfo.Text;

                    VChatPlugin.ReceivedMessageInfo.TryRemove(messageInfoPair.Key, out UserMessageInfo _);
                    return;
                }
            }
        }
    }
}
