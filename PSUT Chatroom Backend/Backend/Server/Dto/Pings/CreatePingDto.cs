using Server.Dto.Users;

namespace Server.Dto.Pings
{
    public class CreatePingDto
    {
        public string Content { get; set; }
        public int RecipientId { get; set; }
    }
}