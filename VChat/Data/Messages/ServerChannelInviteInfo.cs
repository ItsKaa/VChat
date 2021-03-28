namespace VChat.Data.Messages
{
    public class ServerChannelInviteInfo
    {
        public ulong InviterId { get; set; }
        public ulong InviteeId { get; set; }
        public string ChannelName { get; set; }

        public ServerChannelInviteInfo()
        {
        }

        public ServerChannelInviteInfo(ServerChannelInviteInfo other)
        {
            InviterId = other.InviterId;
            InviteeId = other.InviteeId;
            ChannelName = other.ChannelName;
        }
    }
}
