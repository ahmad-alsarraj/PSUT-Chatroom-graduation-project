namespace Server.Dto.Groups
{
    public class UpdateGroupDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? GroupPictureJpgBase64 { get; set; }
        public bool? DeletePicture { get; set; }
    }
}