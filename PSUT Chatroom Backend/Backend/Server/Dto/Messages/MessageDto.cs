namespace Server.Dto.Messages
{
    public class MessageDto : MessageMetadataDto
    {
        public MessageDeliveryInfoDto[] DeliveryInfo { get; set; }
    }
}