using System;
using Server.Dto.Users;

namespace Server.Dto.Messages
{
    public class MessageDeliveryInfoDto
    {
        public DateTimeOffset ReadingTime { get; set; }
        public int MessageId { get; set; }
        public UserMetadataDto Recipient { get; set; }
    }
}