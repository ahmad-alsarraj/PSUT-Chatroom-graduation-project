using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.GroupMembers;
using Server.Services.UserSystem;

namespace Server.Validators.GroupMembers
{
    public class UpdateGroupMemberDtoValidator : AbstractValidator<UpdateGroupMemberDto>
    {
        public UpdateGroupMemberDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
        {
            RuleFor(d => d.Id)
                .MustAsync(async (id, _) => (await dbContext.GroupsMembers.FindAsync(id).ConfigureAwait(false)) != null)
                .WithMessage("GroupMember(Id: {PropertyValue}) doesn't exist.")
                .MustAsync(async (id, _) =>
                {
                    var user = httpContext.HttpContext!.GetUser();
                    if (user == null) { return false; }
                    var groupId = (await dbContext.GroupsMembers.FindAsync(id).ConfigureAwait(false)).GroupId;
                    var userMembership = await dbContext.GroupsMembers
                        .FirstOrDefaultAsync(gm => gm.UserId == user.Id && gm.GroupId == groupId)
                        .ConfigureAwait(false);
                    return userMembership?.IsAdmin == true;
                })
                .WithMessage("Caller is not an admin in Group(Id: {PropertyValue}).");
        }
    }
}
