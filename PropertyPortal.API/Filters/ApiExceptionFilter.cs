using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace PropertyPortal.API.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            context.Result = exception switch
            {
                // 409 Conflict: Handle your RowVersion/Concurrency errors
                DbUpdateConcurrencyException => new ConflictObjectResult(new
                {
                    message = "The record was modified by another user. Please reload the data."
                }),

                // 404 Not Found: If a repository throws this
                KeyNotFoundException => new NotFoundObjectResult(new { message = exception.Message }),

                // 400 Bad Request: For validation or logic errors
                ArgumentException => new BadRequestObjectResult(new { message = exception.Message }),

                // 500 Internal Server Error: Default for everything else
                _ => new ObjectResult(new { message = "An unexpected error occurred." })
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                }
            };

            context.ExceptionHandled = true;
        }
    }
}
