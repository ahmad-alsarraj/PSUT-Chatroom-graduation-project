namespace Server.Dto.Groups
{
    //TODO: delete ?!!
    public class GroupSearchFilterDto
    {
        public string? NameMask { get; set; }
        public bool MyGroupsOnly { get; set; }
        public int Offset { get; set; }
        public int Count { get; set; }
        public bool Metadata { get; set; }
    }
}