using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Server.Db.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RegnewId { get; set; }
        public ICollection<Section> Sections { get; set; }
        public static void ConfigureEntity(EntityTypeBuilder<Course> b)
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Name)
                .IsRequired()
                .IsUnicode();
            b.Property(u => u.RegnewId)
                .IsRequired()
                .IsUnicode();
        }
    }
}