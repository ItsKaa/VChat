namespace VChat.Data
{
    public enum GlobalMessageType
    {
        /// <summary>
        /// A normal global message.
        /// </summary>
        StandardMessage,

        /// <summary>
        /// Messages for unconnected VChat users that are redirected to the global chat channel.
        /// Currently just local chat
        /// </summary>
        RedirectedGlobalMessage,
    }
}
