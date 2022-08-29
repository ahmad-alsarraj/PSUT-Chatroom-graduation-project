using System;
using System.IO;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.Groups;
using Server.Dto.Messages;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;

namespace Server.Validators.Groups
{
    public class CreateMessageDtoValidator : AbstractValidator<CreateMessageDto>
    {
        public CreateMessageDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
        {
            RuleFor(d => d.Content)
                .NotEmpty()
                .When(d => d.Attachment == null)
                .WithMessage("{PropertyName} can't be empty if the message has no attachment.");
            RuleFor(d => d.ReferencedMessageId)
                .MustAsync(async (id, _) => (await dbContext.Messages.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("Message(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (d, id, _) => (await dbContext.Messages.FindAsync(id).ConfigureAwait(false)).ConversationId == d.ConversationId)
                .WithMessage(d => $"ReferencedMessage(Id: {{PropertyValue}}) doesn't belong to the same Conversation(Id: {d.ConversationId})")
                .When(d => d.ReferencedMessageId != null);
            RuleFor(d => d.Attachment)
                .InjectValidator();
            RuleFor(d => d.ConversationId)
                .MustAsync(async (id, _) => (await dbContext.Conversations.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("Conversation(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (id, _) =>
                {
                    var caller = httpContext.HttpContext!.GetUser();
                    if (caller == null) { return false; }
                    var conversation = await dbContext.Conversations
                        .Include(c => c.Members)
                        .FirstAsync(c => c.Id == id)
                        .ConfigureAwait(false);
                    if (conversation.IsClosed) { return false; }
                    if (conversation.IsDirect) { return conversation.Members.Any(m => m.Id == caller.Id); }
                    return await dbContext.GroupsMembers.AnyAsync(gm => gm.GroupId == conversation.GroupId && gm.UserId == caller.Id).ConfigureAwait(false);
                })
                .WithMessage("Converstaion(Id: {PropertyValue}) is closed or the caller is not a member of it.");
        }
    }
}
