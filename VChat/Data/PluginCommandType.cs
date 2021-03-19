namespace VChat.Data
{
    /// <summary>
    /// Command types for commands that should be searchable, primarily needed for chat channels.
    /// </summary>
    public enum PluginCommandType
    {
        None = -1,
        SendLocalMessage = 0,
        SendShoutMessage,
        SendWhisperMessage,
        SendGlobalMessage,
    }
}
