using AutoMapper;
using Server.Db.Entities;
using Server.Dto.Pings;

namespace Server.MappingProfiles
{
    public class PingProfile : Profile
    {
        public PingProfile()
        {
            CreateMap<Ping, PingDto>();
        }
    }
}