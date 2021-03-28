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
        public string ServerCommandName => Name.StripRichTextFormatting().StripWhitespaces().ToLower();
        public Color Color { get; set; }

        public ServerChannelInfo()
        {
            Name = string.Empty;
            OwnerId = 0UL;
            IsPublic = false;
            IsPluginOwnedChannel = false;
            Invitees = new List<ulong>();
            Color = Color.white;
        }

        public ServerChannelInfo(ServerChannelInfo other)
        {
            Update(other);
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

        /// <summary>
        /// Returns a list of users that have access to this channel, used for sending messages or finding peers.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ulong> GetSteamIds()
        {
            return
                // Known invitees
                Invitees.ToList()
                // Add Onwer ID
                .Concat(new[] { OwnerId })
                // Remove id 0
                .Where(x => x != 0UL)
                // Unique ids in case of duplicates.
                .Distinct();
        }
    }
}
