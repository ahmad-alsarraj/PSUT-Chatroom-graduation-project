using System;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Server.Db;
using Server.Db.Entities;
using Server.Dto.Users;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;
namespace Server.Validators.Users;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator(AppDbContext dbContext, IOptions<AppOptions> appOptions)
    {
        RuleFor(d => d.Email)
            .EmailAddress()
            .Must(e => e.EndsWith(appOptions.Value.UniversityEmailDomain, StringComparison.OrdinalIgnoreCase))
            .WithMessage("The email doesn't belong to the university domain.")
            .WithMessage("Non instructor can only update himself.");
        RuleFor(d => d.GoogleToken)
            .NotEmpty();
    }
}
