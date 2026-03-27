using Microsoft.AspNetCore.Http;
using PropertyPortal.Application.Common.Interfaces;
using System.Security.Claims;

namespace PropertyPortal.Infrastructure.Tenancy
{
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetTenantId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return Guid.Empty;

            // 1. Try the Header (What your React app is sending right now)
            var headerValue = context.Request.Headers["X-Tenant-Id"].ToString();
            if (Guid.TryParse(headerValue, out var headerId)) return headerId;

            // 2. Fallback to the Claim (What your JWT will provide later)
            var claimValue = context.User?.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(claimValue, out var claimId)) 
                return claimId;

            return Guid.Empty;

            //// original code
            //var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            //return Guid.TryParse(claimValue, out var tenantId) ? tenantId : Guid.Empty;
        }

        public Guid? GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value; // fallback to raw 'sub'

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            return null; 
        }
    }
}
