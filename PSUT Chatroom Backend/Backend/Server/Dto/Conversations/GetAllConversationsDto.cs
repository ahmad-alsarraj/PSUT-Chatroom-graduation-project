namespace Server.Dto.Conversations;
public class GetAllConversationsDto
{
    public int Offset { get; set; }
    public int Count { get; set; }
    public bool IsDirect { get; set; }
}