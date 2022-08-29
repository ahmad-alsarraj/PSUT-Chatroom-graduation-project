using System;
using Server.Dto.Users;

namespace Server.Dto.Messages
{
    public class MessageMetadataDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public string? AttachmentFileName { get; set; }
        public DateTimeOffset SendingTime { get; set; }
        public int ConversationId { get; set; }
        public UserMetadataDto Sender { get; set; }
        public int? ReferencedMessageId { get; set; }
    }
}