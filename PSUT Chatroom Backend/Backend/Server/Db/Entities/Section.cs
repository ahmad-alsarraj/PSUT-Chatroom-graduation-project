using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Db.Entities
{
    [Flags]
    public enum SectionDay : short
    {
        Saturday, Sunday, Monday, Tuesday, Wednesday, Thursday
    }
    public class Section
    {
        public int Id { get; set; }
        public string RegnewId { get; set; }
        public TimeSpan Time { get; set; }
        public SectionDay Days { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public static void ConfigureEntity(EntityTypeBuilder<Section> b)
        {
            b.HasKey(s => s.Id);
            b.Property(u => u.RegnewId)
                .IsRequired()
                .IsUnicode();
            b.Property(s => s.Time)
                .IsRequired();
            b.Property(s => s.Days)
                .HasConversion<short>();
            b.HasOne(s => s.Course)
                .WithMany(c => c.Sections)
                .HasForeignKey(s => s.CourseId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(s => s.Group)
                .WithOne(g => g.Section)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}