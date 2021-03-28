using System;
using UnityEngine;

namespace VChat.Data.Events
{
    public class CustomChannelChatMessageEventArgs : EventArgs
    {
        public string ChannelName { get; set; }
        public long PeerId { get; set; }
        public string CallerName { get; set; }
        public string Text { get; set; }
        public Vector3 Position { get; set; }
    }
}
