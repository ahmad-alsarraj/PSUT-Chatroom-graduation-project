using System;
using Server.Dto.Users;

namespace Server.Dto.Messages;

public class GetInConversationDto
{
    public int ConversationId { get; set; }
    public int Offset { get; set; }
    public int Count { get; set; }
}
