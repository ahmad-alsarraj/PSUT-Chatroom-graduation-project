using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.Conversations;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;

namespace Server.Validators.Conversations
{
    public class GetAllConversationsDtoValidator : AbstractValidator<GetAllConversationsDto>
    {
        public GetAllConversationsDtoValidator()
        {
            RuleFor(d => d.Count)
                .GreaterThan(0);
            RuleFor(d => d.Offset)
                .GreaterThanOrEqualTo(0);
        }
    }
}
