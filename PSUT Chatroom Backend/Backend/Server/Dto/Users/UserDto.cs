using RegnewCommon;

namespace Server.Dto.Users
{
    public class UserDto : UserMetadataDto
    {
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }
}