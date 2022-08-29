using RegnewCommon;

namespace Server.Dto.Users
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ProfilePictureJpgBase64 { get; set; }
        public bool? DeletePicture { get; set; }
    }
}