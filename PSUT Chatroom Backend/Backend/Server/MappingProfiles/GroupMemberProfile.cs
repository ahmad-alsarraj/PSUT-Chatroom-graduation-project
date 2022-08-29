using AutoMapper;
using Server.Db.Entities;
using Server.Dto.GroupMembers;

namespace Server.MappingProfiles
{
    public class GroupMemberProfile : Profile
    {
        public GroupMemberProfile()
        {
            CreateMap<GroupMember, GroupMemberDto>();
        }
    }
}