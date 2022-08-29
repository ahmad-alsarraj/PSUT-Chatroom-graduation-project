using AutoMapper;
using Server.Db.Entities;
using Server.Dto.Courses;

namespace Server.MappingProfiles
{
    public class CourseProfile : Profile
    {
        public CourseProfile()
        {
            CreateMap<Course, CourseDto>();
            CreateMap<Course, CourseMetadataDto>();
        }
    }
}