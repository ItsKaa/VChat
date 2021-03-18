﻿using System.Collections.Generic;
using UnityEngine;

namespace VChat.Data.Messages
{
    public class ServerChannelInfo
    {
        public string Name { get; set; }
        public ulong OwnerId { get; set; }
        public bool IsPublic { get; set; }
        public List<ulong> Invitees { get; set; }
        public string ServerCommandName { get; set; }
        public Color Color { get; set; }

        public ServerChannelInfo()
        {
            Name = string.Empty;
            OwnerId = 0;
            IsPublic = false;
            Invitees = new List<ulong>();
            Color = Color.white;
        }
    }
}