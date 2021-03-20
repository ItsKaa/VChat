using System;
using System.Linq;
using UnityEngine;
using VChat.Data;
using VChat.Extensions;
using VChat.Messages;
using VChat.Services;

namespace VChat
{
    public partial class VChatPlugin
    {
        public static CommandHandler CommandHandler { get; set; }


        private static void InitialiseClientCommands()
        {
            lock (_commandHandlerLock)
            {
                CommandHandler.ClearCommands();

                const string changedColorMessageSuccess = "Changed the {0} color to <color={1}>{2}</color>.";
                const string errorParseColorMessage = "Could not parse the color \"{0}\".";
                const string errorParseNumber = "Could not convert \"{0}\" to a valid number.";

                var writeErrorMessage = new Action<string>((string message) =>
                {
                    Chat.instance.AddString($"<color=red>[{Name}][Error] {message}</color>");
                });

                var writeSuccessMessage = new Action<string>((string message) =>
                {
                    Chat.instance.AddString($"<color=#23ff00>[{Name}] {message}</color>");
                });

                var applyChannelColorCommand = new Func<string, CombinedMessageType, Color?>((string text, CombinedMessageType messageType) =>
                {
                    text = text?.Trim();
                    var color = text?.ToColor();

                    // Get default color if text is empty.
                    if (string.IsNullOrEmpty(text))
                    {
                        text = "default";
                        color = GetTextColor(messageType, true);
                    }

                    // Write the response message.
                    if (color != null)
                    {
                        writeSuccessMessage(string.Format(changedColorMessageSuccess, messageType.ToString().ToLower(), color?.ToHtmlString(), text));
                    }
                    else
                    {
                        writeErrorMessage(string.Format(errorParseColorMessage, text));
                    }
                    return color;
                });


                CommandHandler.AddCommands(
                    new PluginCommandClient(PluginCommandType.SendLocalMessage, Settings.LocalChatCommandName, (text, instance) =>
                    {
                        LastChatType.Set(Talker.Type.Normal);
                        if (!string.IsNullOrEmpty(text))
                        {
                            ((Chat)instance).SendText(Talker.Type.Normal, text);
                        }
                    }),
                    new PluginCommandClient(PluginCommandType.SendShoutMessage, Settings.ShoutChatCommandName, (text, instance) =>
                    {
                        LastChatType.Set(Talker.Type.Shout);
                        if (!string.IsNullOrEmpty(text))
                        {
                            ((Chat)instance).SendText(Talker.Type.Shout, text);
                        }
                    }),
                    new PluginCommandClient(PluginCommandType.SendWhisperMessage, Settings.WhisperChatCommandName, (text, instance) =>
                    {
                        LastChatType.Set(Talker.Type.Whisper);
                        if (!string.IsNullOrEmpty(text))
                        {
                            ((Chat)instance).SendText(Talker.Type.Whisper, text);
                        }
                    }),
                    new PluginCommandClient(PluginCommandType.SendGlobalMessage, Settings.GlobalChatCommandName, (text, instance) =>
                    {
                        LastChatType.Set(CustomMessageType.Global);
                        if (!string.IsNullOrEmpty(text))
                        {
                            GlobalMessages.SendGlobalMessageToServer(text);
                        }
                    }),
                    new PluginCommandClient(Settings.SetLocalChatColorCommandName, (text, instance) =>
                    {
                        var color = applyChannelColorCommand(text, new CombinedMessageType(Talker.Type.Normal));
                        if (color != null)
                        {
                            Settings.LocalChatColor = color;
                        }
                    }),
                    new PluginCommandClient(Settings.SetShoutChatColorCommandName, (text, instance) =>
                    {
                        var color = applyChannelColorCommand(text, new CombinedMessageType(Talker.Type.Shout));
                        if (color != null)
                        {
                            Settings.ShoutChatColor = color;
                        }
                    }),
                    new PluginCommandClient(Settings.SetWhisperChatColorCommandName, (text, instance) =>
                    {
                        var color = applyChannelColorCommand(text, new CombinedMessageType(Talker.Type.Whisper));
                        if (color != null)
                        {
                            Settings.WhisperChatColor = color;
                        }
                    }),
                    new PluginCommandClient(Settings.GlobalWhisperChatColorCommandName, (text, instance) =>
                    {
                        var color = applyChannelColorCommand(text, new CombinedMessageType(CustomMessageType.Global));
                        if (color != null)
                        {
                            Settings.GlobalChatColor = color;
                        }
                    }),
                    new PluginCommandClient(Settings.ShowChatCommandName, (text, instance) =>
                    {
                        Settings.AlwaysShowChatWindow = !Settings.AlwaysShowChatWindow;
                        writeSuccessMessage($"{(Settings.AlwaysShowChatWindow ? "Always displaying" : "Auto hiding")} chat window.");
                    }),
                    new PluginCommandClient(Settings.ShowChatOnMessageCommandName, (text, instance) =>
                    {
                        Settings.ShowChatWindowOnMessageReceived = !Settings.ShowChatWindowOnMessageReceived;
                        writeSuccessMessage($"{(Settings.ShowChatWindowOnMessageReceived ? "Displaying" : "Not displaying")} chat window when receiving a message.");
                    }),
                    new PluginCommandClient(Settings.ChatClickThroughCommandName, (text, instance) =>
                    {
                        Settings.EnableClickThroughChatWindow = !Settings.EnableClickThroughChatWindow;
                        writeSuccessMessage($"{(Settings.EnableClickThroughChatWindow ? "Enabled" : "Disabled")} clicking through the chat window.");
                        ((Chat)instance).m_chatWindow?.ChangeClickThroughInChildren(!Settings.EnableClickThroughChatWindow);
                    }),
                    new PluginCommandClient(Settings.MaxPlayerChatHistoryCommandName, (text, instance) =>
                    {
                        if (string.IsNullOrEmpty(text))
                        {
                            writeSuccessMessage($"The number of stored player messages is set to {Settings.MaxPlayerMessageHistoryCount}.");
                        }
                        else
                        {
                            if (ushort.TryParse(text, out ushort value))
                            {
                                Settings.MaxPlayerMessageHistoryCount = value;
                                if (value > 0)
                                {
                                    writeSuccessMessage($"Changed the maximum stored player messages to {value}.");
                                }
                                else
                                {
                                    writeSuccessMessage($"Disabled capturing player chat history.");
                                    MessageSendHistory.Clear();
                                }

                                // Readjust buffer size
                                while (MessageSendHistory.Count > 0 && MessageSendHistory.Count < Settings.MaxPlayerMessageHistoryCount)
                                {
                                    MessageSendHistory.RemoveAt(0);
                                }
                            }
                            else
                            {
                                writeErrorMessage(string.Format(errorParseNumber, text));
                            }
                        }
                    }),
                    new PluginCommandClient(Settings.SetChatHideDelayCommandName, (text, instance) =>
                    {
                        if (float.TryParse(text, out float delay) && !float.IsNaN(delay))
                        {
                            if (delay > 0)
                            {
                                Settings.ChatHideDelay = delay;
                                ((Chat)instance).m_hideDelay = delay;
                                writeSuccessMessage($"Updated the chat hide delay to {delay} seconds.");
                            }
                            else
                            {
                                writeErrorMessage($"Hide delay must be greater than 0.");
                            }
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    }),
                    new PluginCommandClient(Settings.SetChatFadeTimeCommandName, (text, instance) =>
                    {
                        if (float.TryParse(text, out float time) && !float.IsNaN(time))
                        {
                            time = Math.Max(0f, time);
                            writeSuccessMessage($"Updated the chat fade timer to {time} seconds.");
                            Settings.ChatFadeTimer = time;
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    }),
                    new PluginCommandClient(Settings.SetOpacityCommandName, (text, instance) =>
                    {
                        if (uint.TryParse(text, out uint opacity))
                        {
                            opacity = Math.Min(100, Math.Max(0, opacity));
                            writeSuccessMessage($"Updated the chat opacity to {opacity}.");
                            Settings.ChatOpacity = opacity;
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    }),
                    new PluginCommandClient(Settings.SetInactiveOpacityCommandName, (text, instance) =>
                    {
                        if (uint.TryParse(text, out uint opacity))
                        {
                            opacity = Math.Min(100, Math.Max(0, opacity));
                            writeSuccessMessage($"Updated the chat inactive opacity to {opacity}.");
                            Settings.InactiveChatOpacity = opacity;
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    }),
                    new PluginCommandClient(Settings.SetDefaultChatChannelCommandName, (text, instance) =>
                    {
                        var type = new CombinedMessageType(CustomMessageType.Global);
                        bool success = false;

                        if (Enum.TryParse(text, true, out Talker.Type talkerType)
                            && talkerType != Talker.Type.Ping)
                        {
                            type.Set(talkerType);
                            success = true;
                        }
                        else
                        {
                            if (Enum.TryParse(text, true, out CustomMessageType customType))
                            {
                                type.Set(customType);
                                success = true;
                            }
                        }

                        if (success)
                        {
                            writeSuccessMessage($"Updated the default chat channel to {text}.");
                            Settings.DefaultChatChannel = type;
                        }
                        else
                        {
                            writeErrorMessage($"Failed to convert \"{text}\" into a chat channel name. Accepted values: {string.Join(", ", Enum.GetNames(typeof(Talker.Type)).Except(new[] { nameof(Talker.Type.Ping) }).Concat(Enum.GetNames(typeof(CustomMessageType))).Distinct().Select(x => x.ToLower()))}.");
                        }
                    }),
                    new PluginCommandClient(Settings.SetWidthCommandName, (text, instance) =>
                    {
                        // Set to default if no argument is provided
                        if (string.IsNullOrEmpty(text))
                        {
                            text = "500";
                        }

                        if (uint.TryParse(text, out uint value))
                        {
                            Settings.ChatWidth = value;
                            ((Chat)instance)?.UpdateChatSize(new Vector2(Settings.ChatWidth, Settings.ChatHeight));

                            writeSuccessMessage($"Updated the chat width to {value}.");
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    }),
                    new PluginCommandClient(Settings.SetHeightCommandName, (text, instance) =>
                    {
                        // Set to default if no argument is provided
                        if (string.IsNullOrEmpty(text))
                        {
                            text = "400";
                        }

                        if (uint.TryParse(text, out uint value))
                        {
                            Settings.ChatHeight = value;
                            ((Chat)instance)?.UpdateChatSize(new Vector2(Settings.ChatWidth, Settings.ChatHeight));

                            writeSuccessMessage($"Updated the chat height to {Settings.ChatHeight}.");
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    }),
                    new PluginCommandClient(Settings.SetBufferSizeCommandName, (text, instance) =>
                    {
                        // Set to default if no argument is provided
                        if (string.IsNullOrEmpty(text))
                        {
                            text = "15";
                        }

                        if (uint.TryParse(text, out uint value))
                        {
                            Settings.ChatBufferSize = value;
                            var chatWindow = ((Chat)instance)?.m_chatWindow;

                            writeSuccessMessage($"Updated the chat buffer size to {value}.");
                        }
                        else
                        {
                            writeErrorMessage(string.Format(errorParseNumber, text));
                        }
                    })
                );
            }
        }

        /// <summary>
        /// Initialise the server command handler, this should be called when ZNet is initialised.
        /// </summary>
        internal static void InitialiseServerCommands()
        {
            lock (_commandHandlerLock)
            {
                Log("Initialising server-wide commands");
                RemoveServerCommands();

                if (ZNet.instance?.IsServer() == true)
                {
                    CommandHandler.AddCommands(
                        new PluginCommandServer("addchannel", (text, peer, steamId) =>
                        {
                            LogWarning($"Got addchannel from local chat");
                            var channelName = text.Trim();
                            ServerChannelManager.ClientSendAddChannelToServer(peer.m_uid, steamId, channelName);
                        }),
                        new PluginCommandServer("invite", (text, peer, steamId) =>
                        {
                            var remainder = text.Trim();
                            var remainderData = text.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                            if (remainderData.Length >= 2)
                            {
                                LogWarning($"Got invite from local chat");
                                var channelName = remainderData[0];
                                var inviteePlayerName = remainderData[1];

                                foreach (var targetPeer in ZNet.instance.GetConnectedPeers())
                                {
                                    if (string.Equals(targetPeer.m_playerName, inviteePlayerName, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        if (targetPeer.m_socket is ZSteamSocket targetSteamSocket)
                                        {
                                            ServerChannelManager.InvitePlayerToChannel(channelName,
                                                peer.m_uid,
                                                steamId,
                                                targetSteamSocket.GetPeerID().m_SteamID
                                            );
                                            ChannelInviteMessage.SendFailedResponseToPeer(peer.m_uid, ChannelInviteMessage.ChannelInviteResponseType.UserNotFound, channelName);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                LogWarning($"Invite from local chat wrong command: \"{text}\" | \"{remainder}\" | \"{string.Join(",", remainderData)}\"");
                            }
                        }),
                        new PluginCommandServer("accept", (text, peer, steamId) =>
                        {
                            LogWarning($"Got accept from local chat");
                            var channelName = text.Trim();
                            if (string.IsNullOrEmpty(channelName))
                            {
                                var invites = ServerChannelManager.GetChannelInvitesForUser(steamId);
                                if (invites?.Count() > 0)
                                {
                                    ServerChannelManager.AcceptChannelInvite(peer.m_uid, invites.FirstOrDefault().ChannelName);
                                }
                                else
                                {
                                    ChannelInviteMessage.SendFailedResponseToPeer(peer.m_uid, ChannelInviteMessage.ChannelInviteResponseType.NoInviteFound, channelName);
                                }
                            }
                            else
                            {
                                ServerChannelManager.AcceptChannelInvite(peer.m_uid, channelName);
                            }
                        }),
                        new PluginCommandServer("decline", (text, peer, steamId) =>
                        {
                            LogWarning($"Got decline from local chat");
                            var channelName = text.Trim();
                            if (string.IsNullOrEmpty(channelName))
                            {
                                var invites = ServerChannelManager.GetChannelInvitesForUser(steamId);
                                if (invites?.Count() > 0)
                                {
                                    ServerChannelManager.DeclineChannelInvite(peer.m_uid, invites.FirstOrDefault().ChannelName);
                                }
                                else
                                {
                                    ChannelInviteMessage.SendFailedResponseToPeer(peer.m_uid, ChannelInviteMessage.ChannelInviteResponseType.NoInviteFound, channelName);
                                }
                            }
                            else
                            {
                                ServerChannelManager.DeclineChannelInvite(peer.m_uid, channelName);
                            }
                        })
                    );
                }

                InitialiseServerChannelCommands();
            }
        }

        /// <summary>
        /// Adds all the channel text commands for the server sided command handler
        /// </summary>
        private static void InitialiseServerChannelCommands()
        {
            lock (_commandHandlerLock)
            {
                if (ZNet.instance?.IsServer() == true)
                {
                    foreach (var channel in ServerChannelManager.GetServerChannelInfoCopy())
                    {
                        if (!string.IsNullOrWhiteSpace(channel.ServerCommandName))
                        {
                            CommandHandler.AddCommand(new PluginCommandServer(channel.ServerCommandName.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries),
                                (text, peer, steamId) =>
                                {
                                    LogWarning($"User {peer.m_playerName} typed in channel {channel.Name} with command {channel.ServerCommandName}");
                                    ServerChannelManager.SendMessageToAllUsersInChannel(peer.m_uid, channel.Name, text);
                                }
                            ));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears the server commands from the command handler, leaves the client commands alone.
        /// </summary>
        private static void RemoveServerCommands()
        {
            lock (_commandHandlerLock)
            {
                foreach (var command in CommandHandler.Commands.OfType<PluginCommandServer>().ToList())
                {
                    CommandHandler.RemoveCommand(command);
                }
            }
        }
    }
}
