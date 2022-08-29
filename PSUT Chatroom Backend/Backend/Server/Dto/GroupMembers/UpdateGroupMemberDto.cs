using Server.Dto.Users;

namespace Server.Dto.GroupMembers
{
    public class UpdateGroupMemberDto
    {
        public int Id { get; set; }
        public bool? IsAdmin { get; set; }
    }
}