namespace RegnewCommon;
public class RegnewCourseDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public RegnewSectionDto[] Sections { get; set; }
}