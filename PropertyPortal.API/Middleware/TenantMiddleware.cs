using PropertyPortal.Domain.Entities;
using System.Security.Claims;

namespace PropertyPortal.API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        // when I try to use the scalar page, i get the following error in TenantMiddleware.cs Invoke method: await _next(context)
        //System.InvalidOperationException: 'CurrentDepth (64) is equal to or larger than the maximum allowed depth of 64. Cannot write the next JSON object or array.'
        public async Task Invoke(HttpContext context)
        {
            var tenantClaim = context.User.FindFirst("TenantId")?.Value;

            // Only parse if we actually found a claim in the JWT
            if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
            {
                context.Items["TenantId"] = tenantId;
            }
            // If no claim, we do nothing. context.Items["TenantId"] remains null.

            await _next(context);
        }

    }
}
