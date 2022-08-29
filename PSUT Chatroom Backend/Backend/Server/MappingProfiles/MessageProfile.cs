using AutoMapper;
using Server.Db.Entities;
using Server.Dto.Messages;

namespace Server.MappingProfiles
{
    public class MessageProfile : Profile
    {
        public MessageProfile()
        {
            CreateMap<Message, MessageDto>();
            CreateMap<Message, MessageMetadataDto>();
        }
    }
}