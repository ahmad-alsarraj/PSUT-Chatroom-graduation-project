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
    public class GetInConversationDtoValidator : AbstractValidator<GetInConversationDto>
    {
        public GetInConversationDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
        {
            RuleFor(d => d.ConversationId)
                .MustAsync(async (id, _) => (await dbContext.Conversations.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("Conversation(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (d, id, _) =>
                {
                    var caller = httpContext.HttpContext!.GetUser();
                    if (caller == null) { return false; }
                    bool isMember = false;
                    var conversation = await dbContext.Conversations.FindAsync(id).ConfigureAwait(false);
                    if (conversation.IsDirect)
                    {
                        isMember = await dbContext.DirectConversationsMembers.AnyAsync(m => m.ConversationId == id && m.UserId == caller.Id).ConfigureAwait(false);
                    }
                    else
                    {
                        isMember = await dbContext.GroupsMembers
                            .Where(gm => gm.GroupId == conversation.GroupId)
                            .AnyAsync(gm => gm.UserId == caller.Id)
                            .ConfigureAwait(false);
                    }
                    return isMember;
                })
                .WithMessage(d => $"Caller is not a member of Conversation(Id: {{PropertyValue}})");
            RuleFor(d => d.Offset)
                .GreaterThanOrEqualTo(0);
            RuleFor(d => d.Count)
                .GreaterThan(0);
        }
    }
}