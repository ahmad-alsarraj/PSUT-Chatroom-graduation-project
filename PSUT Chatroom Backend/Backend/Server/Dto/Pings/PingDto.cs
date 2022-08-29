using Server.Dto.Users;

namespace Server.Dto.Pings
{
    public class PingDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public UserMetadataDto Sender { get; set; }
        public UserMetadataDto Recipient { get; set; }
    }
}