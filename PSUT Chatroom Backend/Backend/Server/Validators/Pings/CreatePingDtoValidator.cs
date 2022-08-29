using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.Pings;
using Server.Services.UserSystem;

namespace Server.Validators.Pings
{
    public class CreatePingDtoValidator : AbstractValidator<CreatePingDto>
    {
        public CreatePingDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
        {
            RuleFor(d => d.Content)
                .NotEmpty();
            RuleFor(d => d.RecipientId)
                .MustAsync(async (id, _) => (await dbContext.Users.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("User(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (id, _) => (await dbContext.Users.FindAsync(id).ConfigureAwait(false)).IsInstructor)
                .WithMessage("User(Id: {PropertyValue}) isn't an instructor.")
                .MustAsync(async (d, _, __) =>
                {
                    var caller = httpContext.HttpContext!.GetUser();
                    if (caller?.IsInstructor == true) { return false; }
                    bool exisitingPing = await dbContext.Pings.AnyAsync(p => p.SenderId == caller.Id && p.RecipientId == d.RecipientId).ConfigureAwait(false);
                    return !exisitingPing;
                })
                .WithMessage("Caller is an instructor or the caller already pinged User(Id: {PropertyValue}).");
        }
    }
}
