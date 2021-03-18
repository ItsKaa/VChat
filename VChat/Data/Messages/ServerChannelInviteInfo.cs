namespace VChat.Data.Messages
{
    public class ServerChannelInviteInfo
    {
        public ulong InviterId { get; set; }
        public ulong InviteeId { get; set; }
        public string ChannelName { get; set; }
    }
}
