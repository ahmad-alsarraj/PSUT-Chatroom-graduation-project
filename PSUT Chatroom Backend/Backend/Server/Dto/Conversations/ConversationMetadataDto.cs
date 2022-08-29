using Server.Dto.Groups;
using Server.Dto.Messages;

namespace Server.Dto.Conversations
{
    public class ConversationMetadataDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsClosed { get; set; }
        public GroupMetadataDto? Group { get; set; }
        public MessageMetadataDto? LastMessage { get; set; }
    }
}