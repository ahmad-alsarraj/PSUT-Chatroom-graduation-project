using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.GroupMembers;
using Server.Services.UserSystem;

namespace Server.Validators.GroupMembers
{
    public class CreateGroupMemberDtoValidator : AbstractValidator<CreateGroupMemberDto>
    {
        public CreateGroupMemberDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
        {
            RuleFor(d => d.GroupId)
                .MustAsync(async (id, _) => (await dbContext.Groups.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("Group(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (id, _) =>
                {
                    var user = httpContext.HttpContext!.GetUser();
                    if (user == null) { return false; }
                    var userMembership = await dbContext.GroupsMembers
                        .FirstOrDefaultAsync(gm => gm.UserId == user.Id && gm.GroupId == id)
                        .ConfigureAwait(false);
                    return userMembership?.IsAdmin == true;
                })
                .WithMessage("Caller is not an admin in Group(Id: {PropertyValue}).");
            RuleFor(d => d.UserId)
                .MustAsync(async (id, _) => (await dbContext.Users.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("User(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (dto, userId, _) => !await dbContext.GroupsMembers.AnyAsync(m => m.GroupId == dto.GroupId && m.UserId == userId).ConfigureAwait(false))
                .WithMessage(dto => $"User(Id: {{PropertyValue}}) is already a member of Group(Id: {dto.GroupId}).");
        }
    }
}
