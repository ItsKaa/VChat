namespace VChat.Data
{
    public struct GreetingMessagePeerInfo
    {
        public long PeerId { get; set; }
        public string Version { get; set; }
        public bool HasReceivedGreeting { get; set; }
        public bool HasSentGreeting { get; set; }
    }
}
