using System;
using System.Collections.Generic;
using System.Linq;
using VChat.Data;
using VChat.Data.Messages;
using VChat.Helpers;

namespace VChat.Services
{
    public class CommandHandler
    {
        private List<PluginCommandBase> _commands;
        public IEnumerable<PluginCommandBase> Commands => _commands;
        public object _lock = new object();

        public CommandHandler()
        {
            _commands = new List<PluginCommandBase>();
        }

        /// <summary>
        /// Adds a command to the collection.
        /// </summary>
        public void AddCommand(PluginCommandBase pluginCommand)
        {
            lock (_lock)
            {
                // First check for duplicate command names.
                foreach (var command in Commands)
                {
                    string duplicateCommandName = null;
                    foreach (var commandName1 in pluginCommand.CommandNames)
                    {
                        foreach (var commandName2 in command.CommandNames)
                        {
                            if (string.Equals(commandName1, commandName2, StringComparison.CurrentCultureIgnoreCase))
                            {
                                VChatPlugin.LogError($"Command with the name \"{VChatPlugin.Settings.CommandPrefix}{duplicateCommandName}\" already exists, types {command.Type} and {pluginCommand.Type}.");
                                duplicateCommandName = commandName1;
                                break;
                            }
                        }
                    }
                }

                // Add the command, we currently don't care if a duplicate command name is found.
                _commands.Add(pluginCommand);
            }
        }

        /// <summary>
        /// Add multiple commands to the collection, see <see cref="AddCommand(PluginCommandBase)"/>.
        /// </summary>
        public void AddCommands(params PluginCommandBase[] pluginCommands)
        {
            foreach (var pluginCommand in pluginCommands)
            {
                AddCommand(pluginCommand);
            }
        }

        /// <summary>
        /// Removes all commands from the collection.
        /// </summary>
        public void ClearCommands()
        {
            lock (_lock)
            {
                _commands.Clear();
            }
        }

        public void RemoveCommand(PluginCommandBase command)
        {
            lock (_lock)
            {
                _commands.Remove(command);
            }
        }

        /// <summary>
        /// Attempt to find the command based on the input, this is case insensitive.
        /// </summary>
        /// <param name="input">The string that comtains the prefix, command name and any arguments that are required</param>
        /// <param name="pluginCommand">The found command, if any.</param>
        /// <param name="remainder">The remainder message of the input, these are the arguments that can be used when executing the action of the command.</param>
        /// <returns>True if successful.</returns>
        public bool TryFindCommand(string input, out PluginCommandBase pluginCommand, out string remainder)
        {
            lock (_lock)
            {
                foreach (var command in Commands)
                {
                    if (IsValidCommandString(input, command, out remainder))
                    {
                        pluginCommand = command;
                        return true;
                    }
                }
            }

            pluginCommand = null;
            remainder = input;
            return false;
        }

        /// <summary>
        /// Compares the input string with the accepted command names for the provided command.
        /// Case insensitive check.
        /// </summary>
        /// <param name="input">The complete string that includes the prefix for the command and any optional arguments</param>
        /// <param name="command">The command that we're comparing for</param>
        /// <param name="remainder">The remainder message of the input</param>
        /// <returns>True if successful</returns>
        public bool IsValidCommandString(string input, PluginCommandBase command, out string remainder)
        {

            foreach (var commandName in command.CommandNames)
            {
                var prefix = "/";
                if (!string.IsNullOrEmpty(commandName)
                   && (input.TrimEnd().Equals($"{prefix}{commandName}", StringComparison.CurrentCultureIgnoreCase)
                   || input.TrimEnd().StartsWith($"{prefix}{commandName} ", StringComparison.CurrentCultureIgnoreCase)))
                {
                    remainder = input.Remove(0, prefix.Length + commandName.Length).TrimStart();
                    return true;
                }

                prefix = VChatPlugin.Settings.CommandPrefix;
                if (!string.IsNullOrEmpty(commandName)
                    && (input.TrimEnd().Equals($"{prefix}{commandName}", StringComparison.CurrentCultureIgnoreCase)
                    || input.TrimEnd().StartsWith($"{prefix}{commandName} ", StringComparison.CurrentCultureIgnoreCase)))
                {
                    remainder = input.Remove(0, prefix.Length + commandName.Length).TrimStart();
                    return true;
                }
            }

            remainder = input;
            return false;
        }

        public bool IsValidCommandString(string input, PluginCommandBase command)
            => IsValidCommandString(input, command, out string _);

        public bool IsValidCommandString(string input, PluginCommandType commandType, out string remainder)
        {
            var command = FindCommand(commandType);
            if (command != null)
            {
                return IsValidCommandString(input, command, out remainder);
            }

            remainder = input;
            return false;
        }

        public bool IsValidCommandString(string input, PluginCommandType commandType)
            => IsValidCommandString(input, commandType, out string _);

        /// <summary>
        /// Attempt to find the command based on the type, this should technically never fail.
        /// </summary>
        public PluginCommandBase FindCommand(PluginCommandType type)
        {
            lock (_lock)
            {
                foreach (var command in Commands)
                {
                    if (command.Type == type)
                    {
                        return command;
                    }
                }
            }

            VChatPlugin.LogError($"Could not find the command for {type}, is this a new unhandled command?");
            return null;
        }

        /// <summary>
        /// Attempt to find the server channel command based on the channel information.
        /// </summary>
        public PluginCommandServerChannel FindCustomChannelCommand(ServerChannelInfo channelInfo)
        {
            lock (_lock)
            {
                foreach (var command in Commands.OfType<PluginCommandServerChannel>())
                {
                    if (ValheimHelper.NameEquals(command.ChannelInfo.Name, channelInfo.Name))
                    {
                        return command;
                    }
                }
            }

            VChatPlugin.LogError($"Could not find the custom channel with {channelInfo.Name}");
            return null;
        }

        /// <summary>
        /// Attempts to find the client command and executes it if found. see <see cref="TryFindCommand(string, out PluginCommandBase, out string)"/>.
        /// </summary>
        /// <param name="executedCommand">The executed command, if any.</param>
        /// <returns>True if successful</returns>
        public bool TryFindAndExecuteClientCommand(string input, object instance, out PluginCommandClient executedCommand)
        {
            if (TryFindCommand(input, out PluginCommandBase command, out string remainder)
                && command is PluginCommandClient clientCommand
                && clientCommand.Method != null)
            {
                clientCommand.Method.Invoke(remainder, instance);
                executedCommand = clientCommand;
                return true;
            }

            executedCommand = null;
            return false;
        }

        /// <summary>
        /// Attempts to find the client command and executes it if found. see <see cref="TryFindCommand(string, out PluginCommandBase, out string)"/>.
        /// </summary>
        /// <param name="executedCommand">The executed command, if any.</param>
        /// <returns>True if successful</returns>
        public bool TryFindAndExecuteClientCommand(string input, out PluginCommandClient executedCommand)
            => TryFindAndExecuteClientCommand(input, null, out executedCommand);

        /// <summary>
        /// Attempts to find the server command and executes it if found. see <see cref="TryFindCommand(string, out PluginCommandBase, out string)"/>.
        /// </summary>
        /// <param name="executedCommand">The executed command, if any.</param>
        /// <returns>True if successful</returns>
        public bool TryFindAndExecuteServerCommand(string input, long peerId, ulong steamId, out PluginCommandServer executedCommand)
        {
            if (TryFindCommand(input, out PluginCommandBase command, out string remainder)
                && command is PluginCommandServer serverCommand
                && serverCommand.Method != null)
            {
                serverCommand.Method.Invoke(remainder, peerId, steamId);
                executedCommand = serverCommand;
                return true;
            }

            executedCommand = null;
            return false;
        }
    }
}
