using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Server.Db.Entities
{
    public class MessageDeliveryInfo
    {
        public DateTimeOffset ReadingTime { get; set; }
        public bool IsDeleted { get; set; }
        public int MessageId { get; set; }
        public Message Message { get; set; }
        public int RecipientId { get; set; }
        public User Recipient { get; set; }

        public static void ConfigureEntity(EntityTypeBuilder<MessageDeliveryInfo> b)
        {
            b.HasKey(m => new { m.MessageId, m.RecipientId });
            b.Property(m => m.ReadingTime)
                .IsRequired();
            b.HasOne(m => m.Message)
                .WithMany(m => m.DeliveryInfo)
                .HasForeignKey(m => m.MessageId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
        public static void CreateSeed(SeedingContext seedingContext)
        {
            List<MessageDeliveryInfo> seed = new();
            Random rand = new();
            var groupsConversations = seedingContext.Conversations.Where(c => c.GroupId != null).ToDictionary(c => c.GroupId!.Value, c => c.Id);
            var groupsMembers = seedingContext
                .GroupsMembers
                .GroupBy(m => m.GroupId)
                .Select<IGrouping<int, GroupMember>, (int ConversationId, int[] MembersIds)>(g => (groupsConversations[g.Key], g.Select(m => m.UserId).ToArray()));

            var directMembers = seedingContext
                .DirectConversationMembers
                .GroupBy(m => m.ConversationId)
                .Select<IGrouping<int, DirectConversationMember>, (int ConversationId, int[] MembersIds)>(g => (g.Key, g.Select(m => m.UserId).ToArray()));
            var conversationsMembers = groupsMembers
                .Concat(directMembers)
                .ToDictionary(g => g.ConversationId, g => g.MembersIds);
            foreach (var message in seedingContext.Messages)
            {
                var members = conversationsMembers[message.ConversationId];
                int maxSkippedMembers = 5;
                foreach (var member in members)
                {
                    if (maxSkippedMembers > 0 && rand.NextBool())
                    {
                        maxSkippedMembers--;
                        continue;
                    }
                    MessageDeliveryInfo inf = new()
                    {
                        IsDeleted = rand.NextBool(),
                        MessageId = message.Id,
                        RecipientId = member,
                        ReadingTime = message.SendingTime + TimeSpan.FromHours(rand.Next(5))
                    };
                    seed.Add(inf);
                }
            }
        }
    }
}