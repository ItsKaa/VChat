using System;
using System.Collections.Generic;
using UnityEngine;
using VChat.Data;

namespace VChat.Services
{
    public class CommandHandler
    {
        private List<PluginCommand> _commands;
        public IEnumerable<PluginCommand> Commands => _commands;
        public object _lock = new object();
        public string Prefix { get; set; }

        public CommandHandler()
        {
            Prefix = "/";
            _commands = new List<PluginCommand>();
        }

        /// <summary>
        /// Adds a command to the collection.
        /// </summary>
        public void AddCommand(PluginCommand pluginCommand)
        {
            lock (_lock)
            {
                // First check for duplicate command names.
                foreach(var command in Commands)
                {
                    string duplicateCommandName = null;
                    foreach (var commandName1 in pluginCommand.CommandNames)
                    {
                        foreach (var commandName2 in command.CommandNames)
                        {
                            if (string.Equals(commandName1, commandName2, StringComparison.CurrentCultureIgnoreCase))
                            {
                                VChatPlugin.LogError($"Command with the name \"{Prefix}{duplicateCommandName}\" already exists, types {command.Type} and {pluginCommand.Type}.");
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
        /// Add multiple commands to the collection, see <see cref="AddCommand(PluginCommand)"/>.
        /// </summary>
        public void AddCommands(params PluginCommand[] pluginCommands)
        {
            foreach(var pluginCommand in pluginCommands)
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

        /// <summary>
        /// Attempt to find the command based on the input, this is case insensitive.
        /// </summary>
        /// <param name="input">The string that comtains the prefix, command name and any arguments that are required</param>
        /// <param name="pluginCommand">The found command, if any.</param>
        /// <param name="remainder">The remainder message of the input, these are the arguments that can be used when executing the action of the command.</param>
        /// <returns>True if successful.</returns>
        public bool TryFindCommand(string input, out PluginCommand pluginCommand, out string remainder)
        {
            lock (_lock)
            {
                foreach (var command in Commands)
                {
                    if(IsValidCommandString(input, command, out remainder))
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
        public bool IsValidCommandString(string input, PluginCommand command, out string remainder)
        {
            foreach (var commandName in command.CommandNames)
            {
                if (!string.IsNullOrEmpty(commandName)
                    && (input.TrimEnd().Equals($"{Prefix}{commandName}", StringComparison.CurrentCultureIgnoreCase)
                    || input.TrimEnd().StartsWith($"{Prefix}{commandName} ", StringComparison.CurrentCultureIgnoreCase)))
                {
                    remainder = input.Remove(0, Prefix.Length + commandName.Length).TrimStart();
                    return true;
                }
            }

            remainder = input;
            return false;
        }

        public bool IsValidCommandString(string input, PluginCommand command)
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
        public PluginCommand FindCommand(PluginCommandType type)
        {
            lock (_lock)
            {
                foreach (var command in Commands)
                {
                    if(command.Type == type)
                    {
                        return command;
                    }
                }
            }

            VChatPlugin.LogError($"Could not find the command for {type}, is this a new unhandled command?");
            return null;
        }

        /// <summary>
        /// Attempts to find the command and executes it if found. see <see cref="TryFindCommand(string, out PluginCommand, out string)"/>.
        /// </summary>
        /// <param name="executedCommand">The executed command, if any.</param>
        /// <returns>True if successful</returns>
        public bool TryFindAndExecuteCommand(string input, Chat chat, out PluginCommand executedCommand)
        {
            if (TryFindCommand(input, out PluginCommand command, out string remainder))
            {
                if (command.Method != null)
                {
                    command.Method.Invoke(remainder, chat);
                    executedCommand = command;
                    return true;
                }
            }

            executedCommand = null;
            return false;
        }
    }
}
