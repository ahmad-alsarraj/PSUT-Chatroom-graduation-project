using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Db.Entities
{
    public class Conversation
    {
        public int Id { get; set; }
        public bool IsClosed { get; set; }
        public int? GroupId { get; set; }
        public Group? Group { get; set; }
        public bool IsDirect => !GroupId.HasValue;
        public ICollection<User> Members { get; set; }
        public ICollection<Message> Messages { get; set; }
        public static void ConfigureEntity(EntityTypeBuilder<Conversation> b)
        {
            b.HasKey(c => c.Id);
            b.HasOne(c => c.Group)
                .WithOne(g => g.Conversation)
                .HasForeignKey<Conversation>(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Ignore(c => c.IsDirect);
            b.Property(c => c.IsClosed)
                .HasDefaultValue(false);
            b.HasCheckConstraint("CK_Conversation_IsClosed", $"\"{nameof(IsClosed)}\" = False OR \"{nameof(GroupId)}\" IS NULL");
        }
        public static void CreateSeed(SeedingContext seedingContext)
        {
            Random rand = new();
            int id = seedingContext.Conversations.Count + 1, memberId = seedingContext.DirectConversationMembers.Count + 1;
            var sectionsGroupsIds = seedingContext.Sections.Select(s => s.GroupId).ToHashSet();

            seedingContext.Conversations.AddRange(seedingContext.Groups
                .Where(g => !sectionsGroupsIds.Contains(g.Id))
                .Select(g => new Conversation
                {
                    IsClosed = false,
                    GroupId = g.Id,
                    Id = id++
                }));
            var instructors = seedingContext.Users.Where(u => u.IsInstructor).ToArray();
            var students = seedingContext.Users.Where(u => u.IsStudent).ToArray();
            int cnt;
            foreach (var ins in instructors)
            {
                //if (!rand.NextBool()) { continue; }
                cnt = rand.Next(1, students.Length - 1);
                for (int lastIdx = students.Length - 1; cnt > 0; cnt--)
                {
                    var student = rand.NextElementAndSwap(students, lastIdx);
                    lastIdx--;
                    Conversation conv = new()
                    {
                        GroupId = null,
                        //IsClosed = rand.NextBool(),
                        IsClosed = false,
                        Id = id++
                    };
                    seedingContext.DirectConversationMembers.Add(new DirectConversationMember
                    {
                        Id = memberId++,
                        ConversationId = conv.Id,
                        UserId = ins.Id,
                    });
                    seedingContext.DirectConversationMembers.Add(new DirectConversationMember
                    {
                        Id = memberId++,
                        ConversationId = conv.Id,
                        UserId = student.Id,
                    });
                    seedingContext.Conversations.Add(conv);
                }
            }
        }
    }
}