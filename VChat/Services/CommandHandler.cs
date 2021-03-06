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
                                duplicateCommandName = commandName1;
                                break;
                            }
                        }
                    }

                    if(duplicateCommandName != null)
                    {
                        Debug.LogError($"Command with the same name already exists: \"{Prefix}{duplicateCommandName}\".");
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
        /// <param name="remainder">The remaining text (arguments) for the string, or input if pluginCommand is null.</param>
        /// <returns>True if successful.</returns>
        public bool TryFindCommand(string input, out PluginCommand pluginCommand, out string remainder)
        {
            // Parse function used in the loop.
            string remainderString = null;
            PluginCommand foundCommand = null;
            var parseFunc = new Func<PluginCommand, bool>((PluginCommand cmd) =>
            {
                foreach (var commandName in cmd.CommandNames)
                {
                    if (!string.IsNullOrEmpty(commandName)
                        && input.TrimEnd().StartsWith($"{Prefix}{commandName}"))
                    {
                        remainderString = input.Remove(0, Prefix.Length + commandName.Length).TrimStart();
                        foundCommand = cmd;
                        return true;
                    }
                }

                return false;
            });

            // Loop through every command and attempt to parse the input string.
            lock (_lock)
            {
                foreach (var command in Commands)
                {
                    if (parseFunc(command))
                    {
                        remainder = remainderString;
                        pluginCommand = foundCommand;
                        return true;
                    }
                }
            }

            pluginCommand = null;
            remainder = input;
            return false;
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
