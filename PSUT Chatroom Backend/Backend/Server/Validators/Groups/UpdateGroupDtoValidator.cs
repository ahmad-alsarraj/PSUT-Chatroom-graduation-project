using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.Groups;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;

namespace Server.Validators.Groups
{
    public class UpdateGroupDtoValidator : AbstractValidator<UpdateGroupDto>
    {
        public UpdateGroupDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
        {
            RuleFor(d => d.Id)
              .MustAsync(async (id, _) => (await dbContext.Groups.FindAsync(id).ConfigureAwait(false)) != null)
              .WithMessage("Group(Id: {PropertyValue}) doesn't exist.")
              .MustAsync(async (groupId, _) =>
                {
                    var caller = httpContext.HttpContext!.GetUser();
                    if (caller == null) { return false; }
                    var isAdmin = (await dbContext.GroupsMembers.FirstOrDefaultAsync(gm => gm.UserId == caller.Id && gm.GroupId == groupId).ConfigureAwait(false))?.IsAdmin;
                    return isAdmin == true;
                })
                .WithMessage("Caller is not an admin in Group(Id: {PropertyValue}).");
            RuleFor(d => d.Name)
                    .NotEmpty()
                    .When(d => d.Name != null);
            RuleFor(d => d.GroupPictureJpgBase64)
                    .MustAsync(async (base64, _) => await Utility.ValidateBase64Image(base64).ConfigureAwait(false))
                    .When(d => d.GroupPictureJpgBase64 != null)
                    .WithMessage("{PropertyName} isn't a valid Base64 image.");
        }
    }
}
