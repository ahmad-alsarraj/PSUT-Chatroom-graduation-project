using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Db.Entities
{
    public class Ping
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int SenderId { get; set; }
        public User? Sender { get; set; }
        public int RecipientId { get; set; }
        public User? Recipient { get; set; }
        public static void ConfigureEntity(EntityTypeBuilder<Ping> b)
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Content)
                .IsUnicode()
                .IsRequired();
            b.HasOne(s => s.Sender)
                .WithMany(u => u.SentPings)
                .HasForeignKey(s => s.SenderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(s => s.Recipient)
                .WithMany(u => u.ReceivedPings)
                .HasForeignKey(s => s.RecipientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
        public static void CreateSeed(SeedingContext seedingContext)
        {
            var instructors = seedingContext.Users.Where(u => u.IsInstructor).ToArray();
            var students = seedingContext.Users.Where(u => u.IsStudent).ToArray();
            Random rand = new();
            int id = 1;
            foreach (var student in students)
            {
                var cnt = rand.Next(1, instructors.Length);
                for (int lastIdx = instructors.Length - 1; cnt > 0; cnt--)
                {
                    var ins = rand.NextElementAndSwap(instructors, lastIdx);
                    lastIdx--;
                    Ping ping = new()
                    {
                        Id = id++,
                        Content = rand.NextText(),
                        RecipientId = ins.Id,
                        SenderId = student.Id
                    };
                    seedingContext.Pings.Add(ping);
                }
            }
        }
    }
}