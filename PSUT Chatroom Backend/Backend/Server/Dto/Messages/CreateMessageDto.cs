using System;
using Server.Dto.Users;

namespace Server.Dto.Messages
{
    public class CreateMessageDto
    {
        public string? Content { get; set; }
        public CreateMessageAttachmentDto? Attachment { get; set; }
        public int ConversationId { get; set; }
        public int? ReferencedMessageId { get; set; }
    }
}