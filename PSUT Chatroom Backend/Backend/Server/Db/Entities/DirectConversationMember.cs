using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Db.Entities
{
    public class DirectConversationMember
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }
        public static void ConfigureEntity(EntityTypeBuilder<DirectConversationMember> b)
        {
            b.HasKey(m => m.Id);
        }
    }
}