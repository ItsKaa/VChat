using System;
using System.Collections.Generic;
using System.Linq;

namespace VChat.Data
{
    public class KnownPlayerData
    {
        public ulong SteamId { get; set; }
        public DateTime LastLoginTime { get; set; }
        public string PlayerName { get; set; }
        public List<string> OtherPlayerNames { get; private set; }
        public string LastKnownVChatVersionString { get; set; }

        public KnownPlayerData()
        {
            SteamId = ulong.MaxValue;
            PlayerName = null;
            LastKnownVChatVersionString = null;
            LastLoginTime = DateTime.MinValue;
            OtherPlayerNames = new List<string>();
        }

        public bool UpdatePlayerName(string playerName)
        {
            if (!string.Equals(PlayerName, playerName, StringComparison.CurrentCultureIgnoreCase))
            {
                // Add old name to the collection if it's not empty.
                bool isNewlyRecordedName = false;
                if (!string.IsNullOrEmpty(PlayerName))
                {
                    // For logging
                    if (!OtherPlayerNames.ToList().Any(x => string.Equals(x, playerName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        isNewlyRecordedName = true;
                    }

                    // Always add current name to the collection
                    OtherPlayerNames.Add(PlayerName);
                }

                // Update player name
                OtherPlayerNames.Remove(playerName);
                PlayerName = playerName;

                if(isNewlyRecordedName)
                {
                    VChatPlugin.LogWarning($"Recorded another player name '{playerName}' for id {SteamId}, total recored names for this user: {string.Join(", ", OtherPlayerNames)}");
                }
                return true;
            }

            return false;
        }
    }
}
