using Server.Dto.Users;

namespace Server.Dto.GroupMembers
{
    public class CreateGroupMemberDto
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public bool IsAdmin { get; set; }
    }
}