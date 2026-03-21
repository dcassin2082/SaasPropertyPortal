using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Domain.Common;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Infrastructure.Data;

namespace PropertyPortal.Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ITenantProvider _tenantProvider;

        public BaseRepository(ApplicationDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public virtual IQueryable<T> Query()
        {
            return _context.Set<T>().AsNoTracking(); // Allows for .Include() in the service layer
        }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            // The Global Query Filter we set up earlier handles the TenantId check automatically.
            // If a user tries to access an ID from another tenant, this returns null.
            return await _context.Set<T>().FindAsync(id);
        }

        public virtual async Task<T> PutAsync(Guid id, T entity)
        {
            var existing = await _context.Set<Property>().FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Record not found.");

            // Update the values on the tracked 'existing' object
            _context.Entry(existing).CurrentValues.SetValues(entity);

            // Ensure the ID matches
            entity.Id = id;

            // IMPORTANT: Ensure the TenantId matches the user's context
            // This prevents a user from "injecting" a different TenantId in the JSON body
            entity.TenantId = _tenantProvider.GetTenantId();

            // Audit trail
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _tenantProvider.GetUserId();

            //// Mark as modified. EF Core will include the [RowVersion] in the WHERE clause.
            //_context.Entry(entity).State = EntityState.Modified;

            // Pro-Tip: Exclude 'CreatedAt' and 'CreatedBy' from being overwritten 
            // if they aren't sent back by the frontend.
            _context.Entry(entity).Property(x => x.CreatedAt).IsModified = false;
            _context.Entry(entity).Property(x => x.CreatedBy).IsModified = false;

            //await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> PostAsync(T entity)
        {
            // Force the TenantId from the secure Middleware/Provider
            if(entity is not Tenant)
            {
                entity.TenantId = _tenantProvider.GetTenantId();
            }

            // Set initial audit data (if not using the Interceptor)
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = _tenantProvider.GetUserId() ?? Guid.Empty;

            await _context.Set<T>().AddAsync(entity);
            //await _context.SaveChangesAsync();

            //await _context.SaveChangesWithConcurrencyAsync(); // use extension method to handle concurrency exceptions
            return entity;
        }

        public virtual async Task<T> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) throw new Exception("Not Found");

            // Soft delete logic
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _tenantProvider.GetUserId();

            //await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Properties.AnyAsync(e => e.Id == id);
        }
    }
}
