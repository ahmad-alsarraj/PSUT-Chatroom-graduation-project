using AutoMapper;
using Server.Db.Entities;
using Server.Dto.GroupMembers;
using Server.Dto.Messages;

namespace Server.MappingProfiles
{
    public class MessageDeliveryInfoProfile : Profile
    {
        public MessageDeliveryInfoProfile()
        {
            CreateMap<MessageDeliveryInfo, MessageDeliveryInfoDto>();
        }
    }
}