using Server.Dto.GroupMembers;
using Server.Dto.Sections;

namespace Server.Dto.Groups
{
    public class GroupMetadataDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ConversationId { get; set; }
        public GroupMemberDto[]? Members { set; get; }
        public SectionDto? Section { get; set; }
    }
}