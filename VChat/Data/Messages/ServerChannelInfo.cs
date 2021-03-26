using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VChat.Extensions;

namespace VChat.Data.Messages
{
    public class ServerChannelInfo
    {
        public string Name { get; set; }
        public ulong OwnerId { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPluginOwnedChannel { get; set; }
        public List<ulong> Invitees { get; set; }
        public string ServerCommandName => Name.StripRichTextFormatting().StripWhitespaces();
        public Color Color { get; set; }

        public ServerChannelInfo()
        {
            Name = string.Empty;
            OwnerId = 0;
            IsPublic = false;
            IsPluginOwnedChannel = false;
            Invitees = new List<ulong>();
            Color = Color.white;
        }

        public void Update(ServerChannelInfo other)
        {
            Name = other.Name;
            OwnerId = other.OwnerId;
            IsPublic = other.IsPublic;
            IsPluginOwnedChannel = other.IsPluginOwnedChannel;
            Invitees = other.Invitees.ToList();
            Color = other.Color;
        }
    }
}
