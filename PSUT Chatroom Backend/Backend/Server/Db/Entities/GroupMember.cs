using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Db.Entities;

namespace Server.Db.Entities
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public bool IsAdmin { get; set; }
        public static void ConfigureEntity(EntityTypeBuilder<GroupMember> b)
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.IsAdmin)
                .IsRequired()
                .HasDefaultValue(false);
            b.HasOne(m => m.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
        public static void CreateSeed(SeedingContext seedingContext)
        {
            Random rand = new();
            List<GroupMember> seed = new();
            var instructors = seedingContext.Users.Where(u => u.IsInstructor).ToArray();
            var students = seedingContext.Users.Where(u => u.IsStudent).ToArray();
            var sectionsGroupsIds = seedingContext.Sections.Select(s => s.GroupId).ToHashSet();
            foreach (var group in seedingContext.Groups)
            {
                if (sectionsGroupsIds.Contains(group.Id)) { continue; }
                int adminsCount = rand.Next(1, instructors.Length);
                int studentsCount = rand.Next(1, students.Length);
                for (int lastIdx = instructors.Length - 1; adminsCount > 0; adminsCount--)
                {
                    var ins = rand.NextElementAndSwap(instructors, lastIdx);
                    lastIdx--;
                    GroupMember m = new()
                    {
                        Id = seedingContext.GroupsMembers.Count + 1,
                        GroupId = group.Id,
                        IsAdmin = true,
                        UserId = ins.Id,
                    };
                    seedingContext.GroupsMembers.Add(m);
                }
                for (int lastIdx = students.Length - 1; studentsCount > 0; studentsCount--)
                {
                    var st = rand.NextElementAndSwap(students, lastIdx);
                    lastIdx--;
                    GroupMember m = new()
                    {
                        Id = seedingContext.GroupsMembers.Count + 1,
                        GroupId = group.Id,
                        IsAdmin = false,
                        UserId = st.Id,
                    };
                    seedingContext.GroupsMembers.Add(m);
                }
            }
        }
    }
}