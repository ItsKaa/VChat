using UnityEngine;

namespace VChat.Extensions
{
    public static class ChatExtensions
    {
        public static void UpdateChatSize(this Chat chat, Vector2 size)
        {
            chat.m_chatWindow.sizeDelta = new Vector2(size.x, size.y);
            var textRect = chat.m_input.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.offsetMax = new Vector2(size.x, textRect.offsetMax.y);
            }
        }
    }
}
