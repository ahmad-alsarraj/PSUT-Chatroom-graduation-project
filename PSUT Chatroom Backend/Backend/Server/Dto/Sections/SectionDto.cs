using System;
using Server.Dto.Courses;
using Server.Dto.Groups;

namespace Server.Dto.Sections
{
    public class SectionDto
    {
        public int Id { get; set; }
        public CourseMetadataDto Course { get; set; }
        public int GroupId { get; set; }
        public TimeSpan Time { get; set; }
        public DayOfWeek[] Days { get; set; }
    }
}