using Server.Dto.Groups;
using Server.Dto.Users;

namespace Server.Dto.Conversations
{
    public class ConversationDto : ConversationMetadataDto
    {
        /// <summary>
        /// It will have members only if its direct conversation.
        /// <summary/>
        public UserMetadataDto[] Members { get; set; }
    }
}