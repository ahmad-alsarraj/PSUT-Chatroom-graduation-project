using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.Pings;
using Server.Services.UserSystem;

namespace Server.Validators.Pings
{
    public class UpdatePingDtoValidator : AbstractValidator<UpdatePingDto>
    {
        public UpdatePingDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
        {
            RuleFor(d => d.Id)
                .MustAsync(async (id, _) => (await dbContext.Pings.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("Ping(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (id, _) =>
                {
                    var caller = httpContext.HttpContext!.GetUser();
                    if (caller == null) { return false; }
                    var ping = await dbContext.Pings.FindAsync(id).ConfigureAwait(false);
                    return ping.SenderId == caller.Id;
                })
                .WithMessage("Caller doesn't is not the sender of Ping(Id: {PropertyValue}).");
            RuleFor(d => d.Content)
                .NotEmpty()
                .When(d => d.Content != null);
        }
    }
}
