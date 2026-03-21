using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Domain.Common;

namespace PropertyPortal.Infrastructure.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ITenantProvider _tenantProvider;
        public AuditInterceptor(ITenantProvider tenantProvider) => _tenantProvider = tenantProvider;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var tenantId = _tenantProvider.GetTenantId();
            var entries = eventData.Context.ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    //if (tenantId.HasValue) entry.Entity.TenantId = tenantId.Value;
                    entry.Entity.TenantId = tenantId;
                    // entry.Entity.CreatedBy = ... pull from claims if needed
                }
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // entry.Entity.UpdatedBy = ... 
                }
            }
            return base.SavingChanges(eventData, result);
        }
    }
}
