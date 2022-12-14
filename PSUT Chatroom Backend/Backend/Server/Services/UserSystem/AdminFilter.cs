using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Server.Services.UserSystem
{
    public class AdminFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.GetUser();
            if (user == null)
            {
                context.Result = new ContentResult
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Content = "NON-EXISTENT/INVALID COOKIE/TOKEN",
                    ContentType = "text/plain"
                };
                return;
            }

            if (!user.IsAdmin)
            {
                context.Result = new ContentResult
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Content = "NOT ADMIN",
                    ContentType = "text/plain"
                };
                return;
            }

            context.HttpContext.Features.Set(user);
            base.OnActionExecuting(context);
        }
    }
}