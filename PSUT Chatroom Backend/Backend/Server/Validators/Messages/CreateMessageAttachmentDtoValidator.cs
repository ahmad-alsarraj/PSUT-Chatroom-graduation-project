using System.IO;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Dto.Groups;
using Server.Dto.Messages;
using Server.Services.FilesManagers;

namespace Server.Validators.Groups
{
    public class CreateMessageAttachmentDtoValidator : AbstractValidator<CreateMessageAttachmentDto>
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        public CreateMessageAttachmentDtoValidator()
        {
            RuleFor(d => d.FileName)
                .NotEmpty()
                .Must(fn => fn.IndexOfAny(InvalidFileNameChars) == -1)
                .WithMessage("{PropertyName} contains invalid file name chars.");
            RuleFor(d => d.FileContentBase64)
                .NotEmpty()
                .Must(base64 => Utility.IsBase64String(base64))
                .WithMessage("{PropertyName} is invalid base64 file.");
        }
    }
}