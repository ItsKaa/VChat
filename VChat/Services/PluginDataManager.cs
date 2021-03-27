﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VChat.Data.Messages;
using VChat.Extensions;

namespace VChat.Services
{
    internal static class PluginDataManager
    {
        public const int Version = 1;
#if USEPLAYERPREFS
        public const string PlayerPrefsKeyName = "vchat";
#endif
        internal static bool Save()
        {
            if (ZNet.instance?.IsDedicated() == true || VChatPlugin.IsPlayerHostedServer)
            {
                // Root package, in case we add more content to the world data.
                var package = new ZPackage();

                // Create server data package
                var serverDataPackage = new ZPackage();

                // Write data
                serverDataPackage.Write(Version);
                SerializeChannels(serverDataPackage);

                // Write server package
                package.Write(serverDataPackage);

                try
                {
#if USEPLAYERPREFS
                    var base64String = package.GetBase64();
                    PlayerPrefs.SetString(PlayerPrefsKeyName, base64String);
#else
                    var filePath = GetFilePath();
                    using var fs = new FileStream(filePath, FileMode.OpenOrCreate | FileMode.Truncate);
                    using var writer = new BinaryWriter(fs);
                    writer.Write(package.GetArray());
#endif
                    VChatPlugin.Log("Saved VChat data.");
                }
                catch (Exception ex)
                {
                    VChatPlugin.LogError($"Failed to save VChat data! Error: {ex}");
                    return false;
                }

                return true;
            }
            else
            {
                // Currently not writing anything for players.
                // NOTE: When this is initialised, existing server data will have to be left alone (read and re-write) because the player may switch between hosting and playing as a client.
                //VChatPlugin.LogError("Saving player data is not yet supported for clients, doing nothing.");
                return true;
            }
        }

        internal static bool Read()
        {
            if (ZNet.instance?.IsDedicated() == true || VChatPlugin.IsPlayerHostedServer)
            {
                ZPackage package = null;
                try
                {
#if USEPLAYERPREFS
                    var base64String = PlayerPrefs.GetString(PlayerPrefsKeyName, null);
                    VChatPlugin.LogWarning($"PlayerPrefs {PlayerPrefsKeyName} = {base64String}");
                    package = new ZPackage(base64String);
#else
                    var filePath = GetFilePath();
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        var bytes = File.ReadAllBytes(filePath);
                        package = new ZPackage(bytes);
                    }
#endif
                }
                catch (Exception ex)
                {
                    VChatPlugin.LogError($"Failed to load VChat data! Error: {ex}");
                    return false;
                }

                if (package != null)
                {
                    var serverPackage = package.ReadPackage();
                    var version = serverPackage.ReadInt();

                    DeserializeChannels(serverPackage, version);
                }
                else
                {
                    VChatPlugin.LogWarning("No save data found");
                }

                return true;
            }
            else
            {
                // Currently not reading anything for players.
                // NOTE: When this is initialised, server data will also have to be read because the player may switch between hosting and playing as a client.
                //VChatPlugin.LogError("Reading player data is not yet supported for clients, doing nothing.");
                return true;
            }
        }

        private static void SerializeChannels(ZPackage package)
        {
            var channels = ServerChannelManager.GetServerChannelInfoCollection();
            var channelsToWrite = channels.Where(x => !x.IsPluginOwnedChannel);

            var channelCollectionPackage = new ZPackage();
            channelCollectionPackage.Write(channelsToWrite.Count());

            foreach (var channel in channelsToWrite)
            {
                // Every loop needs its own ZPackage to ensure newer versions will work
                var channelPackage = new ZPackage();

                // Write basic channel information
                channelPackage.Write(channel.Name ?? string.Empty);
                channelPackage.Write(channel.OwnerId);
                channelPackage.Write(channel.IsPublic);
                channelPackage.Write(channel.Color.ToHtmlString() ?? string.Empty);

                // Write the invitee list as its own package
                var channelInviteeCollectionPackage = new ZPackage();
                channelInviteeCollectionPackage.Write(channel.Invitees.Count);
                foreach (var inviteeId in channel.Invitees)
                {
                    // Yet another loop with its own package,
                    // in case we want to add more information to the invitees.
                    var channelInviteePackage = new ZPackage();
                    channelInviteePackage.Write(inviteeId);
                    channelInviteeCollectionPackage.Write(channelInviteePackage);
                }
                channelPackage.Write(channelInviteeCollectionPackage);
                channelCollectionPackage.Write(channelPackage);
            }

            package.Write(channelCollectionPackage);
        }

        private static void DeserializeChannels(ZPackage package, int version)
        {
            var channels = new List<ServerChannelInfo>();

            var channelCollectionPackage = package.ReadPackage();
            var numberOfChannels = channelCollectionPackage.ReadInt();

            for (int i = 0; i < numberOfChannels; i++)
            {
                var invitees = new List<ulong>();

                // Read the package for this channel
                var channelPackage = channelCollectionPackage.ReadPackage();

                // Read basic channel information
                var name = channelPackage.ReadString();
                var ownerId = channelPackage.ReadULong();
                var isPublic = channelPackage.ReadBool();
                var color = channelPackage.ReadString()?.ToColor() ?? Color.white;

                // Read invitees
                var channelInviteeCollectionPackage = channelPackage.ReadPackage();
                var numberOfInvitees = channelInviteeCollectionPackage.ReadInt();
                for (int j = 0; j < numberOfInvitees; j++)
                {
                    var channelInviteePackage = channelInviteeCollectionPackage.ReadPackage();
                    var inviteeId = channelInviteePackage.ReadULong();
                    invitees.Add(inviteeId);
                }

                channels.Add(new ServerChannelInfo()
                {
                    Name = name,
                    OwnerId = ownerId,
                    IsPublic = isPublic,
                    Color = color,
                    Invitees = invitees,
                    IsPluginOwnedChannel = false
                });
            }

            ServerChannelManager.AddChannelsDirect(channels.ToArray());
        }

        internal static string GetFilePath()
        {
            var worldName = ZNet.m_world.m_name;
            if (string.IsNullOrEmpty(worldName))
            {
                VChatPlugin.LogError("World name is empty, cannot continue reading or writing VChat data.");
                return null;
            }

            return Path.Combine(World.GetWorldSavePath(), $"{worldName}.vchat.bin");
        }
    }
}