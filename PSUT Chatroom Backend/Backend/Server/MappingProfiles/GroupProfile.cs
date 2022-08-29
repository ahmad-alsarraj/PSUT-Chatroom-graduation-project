using AutoMapper;
using Server.Db.Entities;
using Server.Dto.Groups;

namespace Server.MappingProfiles
{
    public class GroupProfile : Profile
    {
        public GroupProfile()
        {
            CreateMap<Group, GroupDto>()
                .ForMember(g => g.ConversationId, op => op.MapFrom(g => g.Conversation.Id));
            CreateMap<Group, GroupMetadataDto>()
                .ForMember(g => g.ConversationId, op => op.MapFrom(g => g.Conversation.Id));
        }
    }
}