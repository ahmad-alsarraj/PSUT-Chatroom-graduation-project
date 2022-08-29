namespace RegnewCommon;
public class RegnewSectionDto
{
    public string Id { get; set; }
    public TimeSpan Time { get; set; }
    public DayOfWeek[] Days { get; set; }
    public string InstructorId { get; set; }
    public string[] StudentsIds { get; set; }
}