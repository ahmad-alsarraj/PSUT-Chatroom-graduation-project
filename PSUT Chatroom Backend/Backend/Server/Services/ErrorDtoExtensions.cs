using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Server.Dto;

namespace Server.Services
{
    public static class ErrorDtoExtensions
    {
        public static IMvcBuilder AddInvalidModelStateResponseFactory(this IMvcBuilder b)
        {
            return b.ConfigureApiBehaviorOptions(op =>
                {
                    op.InvalidModelStateResponseFactory = ctx =>
                    {
                        var errors = ctx.ModelState
                            .Where(e => e.Value.Errors.Count > 0)
                            .ToDictionary(e => e.Key, e => (object)e.Value.Errors.Select(e => e.ErrorMessage).ToArray());

                        var error = new ErrorDto
                        {
                            Description = "Invalid model with the following validation errors.",
                            Data = errors
                        };
                        ObjectResult result = new(error)
                        {
                            StatusCode = StatusCodes.Status422UnprocessableEntity,
                            DeclaredType = typeof(ErrorDto),
                        };
                        return result;
                    };
                });
        }
    }
}