using Server.Dto.Users;

namespace Server.Dto.GroupMembers
{
    //Its meant to be embeded inside GroupDto so no need to include GroupMetadataDto
    public class GroupMemberDto
    {
        public UserMetadataDto User { get; set; }
        public bool IsAdmin { get; set; }
        public int GroupId { get; set; }
    }
}