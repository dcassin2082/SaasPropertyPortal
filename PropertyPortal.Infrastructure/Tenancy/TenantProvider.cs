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
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            return Guid.TryParse(claimValue, out var tenantId) ? tenantId : Guid.Empty;
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
