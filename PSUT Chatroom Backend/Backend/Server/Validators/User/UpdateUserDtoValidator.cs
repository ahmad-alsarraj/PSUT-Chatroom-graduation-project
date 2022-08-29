using FluentValidation;
using Microsoft.AspNetCore.Http;
using Server.Db;
using Server.Db.Entities;
using Server.Dto.Users;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;

namespace Server.Validators.Users;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        RuleFor(d => d.Id)
            .MustAsync(async (id, _) => (await dbContext.Users.FindAsync(id).ConfigureAwait(false)) != null)
            .WithMessage("User(Id: {PropertyValue}) doesn't exist.")
            .MustAsync(async (id, _) =>
            {
                var caller = httpContext.HttpContext!.GetUser();
                if (caller == null)
                {
                    return false;
                }
                var target = await dbContext.Users.FindAsync(id).ConfigureAwait(false);
                return caller.Id == id || (caller.IsAdmin && !target.IsAdmin);
            })
            .WithMessage("Non admin can only update himself.");
        RuleFor(d => d.Name)
            .NotEmpty()
            .When(d => d.Name != null);
        RuleFor(d => d.ProfilePictureJpgBase64)
            .MustAsync(async (base64, _) => await Utility.ValidateBase64Image(base64).ConfigureAwait(false))
            .When(d => d.ProfilePictureJpgBase64 != null)
            .WithMessage("{PropertyName} isn't a valid Base64 image.")
            .When(d => d.ProfilePictureJpgBase64 != null);
    }
}
