using System;
using System.Linq;
using AutoMapper;
using Server.Db.Entities;
using Server.Dto.Sections;

namespace Server.MappingProfiles
{
    public class SectionDayConverter : IValueConverter<SectionDay, DayOfWeek[]>
    {
        public DayOfWeek[] Convert(SectionDay days, ResolutionContext context)
        {
            return Enum.GetValues<SectionDay>()
                .Where(d => days.HasFlag(d))
                .Select(d => d switch
                {
                    SectionDay.Sunday => DayOfWeek.Sunday,
                    SectionDay.Monday => DayOfWeek.Monday,
                    SectionDay.Tuesday => DayOfWeek.Tuesday,
                    SectionDay.Wednesday => DayOfWeek.Wednesday,
                    SectionDay.Thursday => DayOfWeek.Thursday,
                    SectionDay.Saturday => DayOfWeek.Saturday,
                    _ => throw new ArgumentException()
                })
                .ToArray();
        }
    }
    public class SectionProfile : Profile
    {
        public SectionProfile()
        {
            CreateMap<Section, SectionDto>()
                .ForMember(d => d.Days, x => x.ConvertUsing<SectionDayConverter, SectionDay>());
        }
    }
}