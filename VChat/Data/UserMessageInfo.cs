using System;
using UnityEngine;

namespace VChat.Data
{
    public class UserMessageInfo : IEquatable<UserMessageInfo>, IEquatable<Chat.WorldTextInstance>
    {
        public GameObject GameObject { get; set; }
        public long SenderID { get; set; }
        public Vector3 Position { get; set; }
        public Talker.Type Type { get; set; }
        public string User { get; set; }
        public string Text { get; set; }

        public bool Equals(UserMessageInfo other)
        {
            return GameObject?.GetInstanceID() == other.GameObject?.GetInstanceID()
                && SenderID == other.SenderID
                && Position == other.Position
                && Type == other.Type
                && string.Equals(User, other.User, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(Text, other.Text, StringComparison.CurrentCultureIgnoreCase)
                ;
        }

        public bool Equals(Chat.WorldTextInstance other)
        {
            return GameObject?.GetInstanceID() == other.m_go?.GetInstanceID()
                && SenderID == other.m_talkerID
                && Position == other.m_position
                && Type == other.m_type
                && string.Equals(User, other.m_name, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(Text, other.m_text, StringComparison.CurrentCultureIgnoreCase)
                ;
        }
    }
}
