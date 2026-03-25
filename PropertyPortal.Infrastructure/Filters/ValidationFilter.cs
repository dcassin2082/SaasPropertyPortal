using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PropertyPortal.Infrastructure.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                // Wrap in your standard API Response structure
                var response = new
                {
                    StatusCode = 400,
                    Message = "Validation failed. Please check the provided data.",
                    Errors = errors // Detailed dictionary of fields and their specific errors
                };

                context.Result = new BadRequestObjectResult(new { Errors = errors });
                return;
            }

            await next();
        }
    }

}
