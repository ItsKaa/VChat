using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VChat.Data.Messages
{
    public class ServerChannelInfo
    {
        public string Name { get; set; }
        public ulong OwnerId { get; set; }
        public bool IsPublic { get; set; }
        public bool ReadOnly { get; set; }
        public List<ulong> Invitees { get; set; }
        public string ServerCommandName { get; set; }
        public Color Color { get; set; }

        public ServerChannelInfo()
        {
            Name = string.Empty;
            OwnerId = 0;
            IsPublic = false;
            ReadOnly = false;
            Invitees = new List<ulong>();
            Color = Color.white;
        }

        public void Update(ServerChannelInfo other)
        {
            Name = other.Name;
            OwnerId = other.OwnerId;
            IsPublic = other.IsPublic;
            ReadOnly = other.ReadOnly;
            Invitees = other.Invitees.ToList();
            ServerCommandName = other.ServerCommandName;
            Color = other.Color;
        }
    }
}
