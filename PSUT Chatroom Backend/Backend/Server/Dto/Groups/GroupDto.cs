using Server.Dto.GroupMembers;
using Server.Dto.Users;

namespace Server.Dto.Groups
{
    public class GroupDto : GroupMetadataDto
    {
        public GroupMemberDto[] Members { get; set; }
    }
}