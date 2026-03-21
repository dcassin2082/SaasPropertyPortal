using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace PropertyPortal.Infrastructure.Web.Filters
{
    public class ConcurrencyExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is DbUpdateConcurrencyException)
            {
                context.Result = new ConflictObjectResult(new
                {
                    message = "The record has been modified by another user. Please reload and try again."
                });
                context.ExceptionHandled = true;
            }
        }
    }
}
