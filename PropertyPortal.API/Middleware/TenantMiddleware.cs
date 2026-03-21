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
