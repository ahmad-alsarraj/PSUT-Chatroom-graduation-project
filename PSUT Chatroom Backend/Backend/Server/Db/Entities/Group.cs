using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Server.Db.Entities;
using Server.Services;
using Server.Services.FilesManagers;
using SkiaSharp;

namespace Server.Db.Entities
{
    public class Group : IFileEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Conversation Conversation { get; set; }
        public Section? Section { get; set; }
        public ICollection<GroupMember>? Members { get; set; }
        public byte[] EncryptionSalt { get; set; }
        public string FileName => $"{Id}.jpg";

        public static void ConfigureEntity(EntityTypeBuilder<Group> b)
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Name)
                .IsRequired()
                .IsUnicode();
            b.Property(g => g.EncryptionSalt)
                .HasConversion(d => Convert.ToBase64String(d), d => Convert.FromBase64String(d))
                .IsRequired()
                .IsUnicode();
            b.Ignore(g => g.FileName);
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
            var fileManager = seedingContext.ServiceProvider.GetRequiredService<GroupFileManager>();

            async Task GenerateGroupPicture(Group g)
            {
                using SKBitmap bmp = new(200, 200);
                using (SKCanvas can = new(bmp))
                {
                    can.Clear(new SKColor((uint)rand.Next(100, int.MaxValue)));
                    can.Flush();
                }
                using var jpgData = bmp.Encode(SKEncodedImageFormat.Jpeg, 100);
                await using var jpgStream = jpgData.AsStream();
                await fileManager.SaveFile(g, jpgStream).ConfigureAwait(false);
            }

            foreach (var group in seedingContext.Groups)
            {
                if (rand.NextBool()) { continue; }
                await GenerateGroupPicture(group).ConfigureAwait(false);
            }
        }
        public static void CreateSeed(SeedingContext seedingContext)
        {
            Random rand = new();
            int cnt = rand.Next(100, 150);
            var saltBae = seedingContext.ServiceProvider.GetRequiredService<SaltBae>();

            for (int i = 1; i <= cnt; i++)
            {
                Group group = new()
                {
                    Id = i,
                    Name = $"Group {i} {rand.NextText(rand.Next(5, 10))}",
                    EncryptionSalt = saltBae.SaltSteak(null, i)
                };
                seedingContext.Groups.Add(group);
            }
        }
    }
}