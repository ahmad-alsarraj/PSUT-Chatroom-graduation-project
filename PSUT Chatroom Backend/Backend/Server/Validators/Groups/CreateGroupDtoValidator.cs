using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.Groups;
using Server.Services.FilesManagers;

namespace Server.Validators.Groups
{
    public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
    {
        public CreateGroupDtoValidator()
        {
            RuleFor(d => d.Name)
                .NotEmpty();
            RuleFor(d => d.GroupPictureJpgBase64)
                .MustAsync(async (base64, _) => await Utility.ValidateBase64Image(base64).ConfigureAwait(false))
                .When(d => d.GroupPictureJpgBase64 != null)
                .WithMessage("{PropertyName} isn't a valid Base64 image.");
        }
    }
}