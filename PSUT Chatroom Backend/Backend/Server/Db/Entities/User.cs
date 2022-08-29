using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;
using SkiaSharp;
using Server.Services;
using RegnewCommon;

namespace Server.Db.Entities
{
    public class User : IFileEntity
    {
        public int Id { get; set; }
        public string RegnewId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public UserRole Role { get; set; }
        public bool IsInstructor => Role.HasFlag(UserRole.Instructor);
        public bool IsStudent => Role.HasFlag(UserRole.Student);
        public bool IsAdmin => Role.HasFlag(UserRole.Admin);
        public string? Token { get; set; }
        public ICollection<Conversation> Conversations { get; set; }

        public ICollection<Ping> SentPings { get; set; }
        public ICollection<Ping> ReceivedPings { get; set; }
        public byte[] EncryptionSalt { get; set; }
        public string FileName => $"{Id}.jpg";

        public static void ConfigureEntity(EntityTypeBuilder<User> b)
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.RegnewId)
                .IsRequired()
                .IsUnicode();
            b.Property(u => u.Email)
                .IsRequired()
                .IsUnicode();
            b.Property(u => u.Name)
                .IsRequired()
                .IsUnicode();
            b.Property(u => u.Token)
                .IsUnicode();
            b.Property(u => u.Role)
                .IsRequired()
                .HasConversion<byte>();
            b.HasMany(u => u.Conversations)
                .WithMany(c => c.Members)
                .UsingEntity<DirectConversationMember>(
                    b => b.HasOne(e => e.Conversation)
                            .WithMany()
                            .HasForeignKey(e => e.ConversationId)
                            .IsRequired()
                            .OnDelete(DeleteBehavior.Cascade),
                    b => b.HasOne(e => e.User)
                            .WithMany()
                            .HasForeignKey(e => e.UserId)
                            .IsRequired()
                            .OnDelete(DeleteBehavior.Cascade));
            b.Property(g => g.EncryptionSalt)
               .HasConversion(d => Convert.ToBase64String(d), d => Convert.FromBase64String(d))
               .IsRequired()
               .IsUnicode();
            b.Ignore(g => g.FileName);
            b.Ignore(g => g.IsAdmin);
            b.Ignore(g => g.IsInstructor);
            b.Ignore(g => g.IsStudent);
        }
        public static async Task CreateSeedFiles(SeedingContext seedingContext)
        {
            using SKPaint paint = new()
            {
                Color = SKColors.Red,
                Style = SKPaintStyle.Fill,
                HintingLevel = SKPaintHinting.Full,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                TextSize = 10,
                StrokeWidth = 10
            };
            Random rand = new();
            var fileManager = seedingContext.ServiceProvider.GetRequiredService<UserFileManager>();

            async Task GenerateProfilePicture(User u)
            {
                using SKBitmap bmp = new(200, 200);
                using (SKCanvas can = new(bmp))
                {
                    can.Clear(new SKColor((uint)rand.Next(100, int.MaxValue)));
                    can.Flush();
                }
                using var jpgData = bmp.Encode(SKEncodedImageFormat.Jpeg, 100);
                await using var jpgStream = jpgData.AsStream();
                await fileManager.SaveFile(u, jpgStream).ConfigureAwait(false);
            }
            var saltBae = seedingContext.ServiceProvider.GetRequiredService<SaltBae>();
            foreach (var user in seedingContext.Users)
            {
                if (rand.NextBool()) { continue; }
                await GenerateProfilePicture(user).ConfigureAwait(false);
            }
        }
    }
}