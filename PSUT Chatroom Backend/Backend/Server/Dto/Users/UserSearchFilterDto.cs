namespace Server.Dto.Users
{
    public class UserSearchFilterDto
    {
        public string? NameMask { get; set; }
        public bool? IsInstructor { get; set; }
        public int Offset { get; set; }
        public int Count { get; set; }
    }
}