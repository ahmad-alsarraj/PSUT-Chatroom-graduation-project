using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Server.Services;
using Server.Services.FilesManagers;
using SkiaSharp;

namespace Server.Db.Entities
{
    public class Message : IFileEntity
    {
        public int Id { get; set; }
        ///<summary> Can be null if attachment is present. </summary>
        public string? Content { get; set; }
        public string? AttachmentFileName { get; set; }
        public DateTimeOffset SendingTime { get; set; }
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }
        public int SenderId { get; set; }
        public User Sender { get; set; }
        public int? ReferencedMessageId { get; set; }
        public Message? ReferencedMessage { get; set; }
        public ICollection<MessageDeliveryInfo> DeliveryInfo { get; set; }
        public byte[] EncryptionSalt { get; set; }
        public string FileName => Id.ToString();
        public static void ConfigureEntity(EntityTypeBuilder<Message> b)
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Content)
                .IsUnicode();
            b.Property(m => m.SendingTime)
                .IsRequired();
            b.Property(m => m.AttachmentFileName)
                .IsUnicode();
            b.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(m => m.ReferencedMessage)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasCheckConstraint("CK_Message_AttachmentFileName", $"\"{nameof(AttachmentFileName)}\" IS NOT NULL OR \"{nameof(Content)}\" IS NOT NULL");

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
            var fileManager = seedingContext.ServiceProvider.GetRequiredService<MessageFileManager>();

            async Task GenerateMessageAttacment(Message msg)
            {
                if (msg.AttachmentFileName!.EndsWith("jpg"))
                {
                    using SKBitmap bmp = new(200, 200);
                    using (SKCanvas can = new(bmp))
                    {
                        can.Clear(new SKColor((uint)rand.Next(100, int.MaxValue)));
                        can.Flush();
                    }

                    using var jpgData = bmp.Encode(SKEncodedImageFormat.Jpeg, 100);
                    await using var jpgStream = jpgData.AsStream();
                    await fileManager.SaveFile(msg, jpgStream).ConfigureAwait(false);
                }
                else
                {
                    await using MemoryStream content = new();
                    await using StreamWriter contentWriter = new(content);
                    await contentWriter.WriteLineAsync($"Message {msg.Id} attachment file.").ConfigureAwait(false);
                    await contentWriter.WriteLineAsync(rand.NextText()).ConfigureAwait(false);
                    await contentWriter.FlushAsync().ConfigureAwait(false);
                    content.Position = 0;
                    await fileManager.SaveFile(msg, content).ConfigureAwait(false);
                }
            }
            foreach (var message in seedingContext.Messages)
            {
                if (message.AttachmentFileName == null) { continue; }
                await GenerateMessageAttacment(message).ConfigureAwait(false);
            }
        }
        public static void CreateSeed(SeedingContext seedingContext)
        {
            var saltBae = seedingContext.ServiceProvider.GetRequiredService<SaltBae>();

            Random rand = new();
            var groupsMembers = seedingContext
                .GroupsMembers
                .GroupBy(m => m.GroupId)
                .ToDictionary(g => g.Key, g => g.Select(g => g.UserId).ToArray());
            var directMembers = seedingContext
                .DirectConversationMembers
                .GroupBy(m => m.ConversationId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.UserId).ToArray());
            int id = 1;
            DateTimeOffset sendingTime = DateTimeOffset.Now - TimeSpan.FromDays(50);
            foreach (var conv in seedingContext.Conversations)
            {
                int[] members = null;
                if ((conv.IsDirect && !directMembers.TryGetValue(conv.Id, out members)) || (!conv.IsDirect && !groupsMembers.TryGetValue(conv.GroupId.Value, out members)))
                {
                    continue;
                }

                int cnt = rand.Next(10, 20);
                for (int i = 0; i < cnt; i++)
                {
                    Message msg = new()
                    {
                        Id = id,
                        Content = $"Message {id} content{Environment.NewLine}{rand.NextText()}",
                        ConversationId = conv.Id,
                        SenderId = rand.NextElement(members),
                        ReferencedMessageId = rand.NextBool() && id > 2 ? rand.Next(1, id) : null,
                        SendingTime = sendingTime,
                        AttachmentFileName = rand.NextBool() ? $"Message {id} attachment.{(rand.NextBool() ? "jpg" : "txt")}" : null,
                        EncryptionSalt = saltBae.SaltSteak(null, id)
                    };
                    id++;
                    sendingTime += TimeSpan.FromMinutes(rand.Next(1, 5));
                    seedingContext.Messages.Add(msg);
                }
            }
        }
    }
}