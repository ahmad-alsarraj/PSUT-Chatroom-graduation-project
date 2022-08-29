using AutoMapper;
using Server.Db.Entities;
using Server.Dto.Users;

namespace Server.MappingProfiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserMetadataDto>();
            CreateMap<User, UserDto>();
        }
    }
}