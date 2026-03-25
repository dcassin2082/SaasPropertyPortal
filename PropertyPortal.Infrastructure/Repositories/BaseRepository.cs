using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.Common.Models;
using PropertyPortal.Domain.Common;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Infrastructure.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using Mapster;

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
        /*System.InvalidOperationException: 'The entity type 'Address' requires a primary key to be defined. If you intended to use
         * a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://*/
        public virtual IQueryable<T> Query()
        {
            return _context.Set<T>().AsNoTracking(); // Allows for .Include() in the service layer
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            // The Global Query Filter we set up earlier handles the TenantId check automatically.
            // If a user tries to access an ID from another tenant, this returns null.
            return  await _context.Set<T>().FindAsync(id);
        }

        public virtual async Task<T> PutAsync(Guid id, T entity)
        {
            var existing = await _context.Set<T>().FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Record not found.");

            // 1. Sync values from incoming entity to the tracked database entity
            _context.Entry(existing).CurrentValues.SetValues(entity);

            // 2. Audit & Security (Modify the 'existing' object directly)
            existing.TenantId = _tenantProvider.GetTenantId();
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = _tenantProvider.GetUserId();

            // 3. Explicitly protect creation fields on the TRACKED object
            var entry = _context.Entry(existing);
            entry.Property(x => x.Id).IsModified = false; // Prevents the PK error
            entry.Property(x => x.CreatedAt).IsModified = false;
            entry.Property(x => x.CreatedBy).IsModified = false;

            // EF will automatically handle RowVersion on 'existing'
            return existing;
            //var existing = await _context.Set<T>().FindAsync(id);
            //if (existing == null) throw new KeyNotFoundException("Record not found.");

            //// Update the values on the tracked 'existing' object
            //_context.Entry(existing).CurrentValues.SetValues(entity);

            //// Ensure the ID matches
            //entity.Id = id;

            //// IMPORTANT: Ensure the TenantId matches the user's context
            //// This prevents a user from "injecting" a different TenantId in the JSON body
            //entity.TenantId = _tenantProvider.GetTenantId();

            //// Audit trail
            //entity.UpdatedAt = DateTime.UtcNow;
            //entity.UpdatedBy = _tenantProvider.GetUserId();

            ////// Mark as modified. EF Core will include the [RowVersion] in the WHERE clause.
            ////_context.Entry(entity).State = EntityState.Modified;

            //// Pro-Tip: Exclude 'CreatedAt' and 'CreatedBy' from being overwritten 
            //// if they aren't sent back by the frontend.
            //_context.Entry(entity).Property(x => x.CreatedAt).IsModified = false;
            //_context.Entry(entity).Property(x => x.CreatedBy).IsModified = false;

            ////await _context.SaveChangesAsync();
            //return entity;
        }

        public virtual async Task<T> PostAsync(T entity)
        {
            // Force the TenantId from the secure Middleware/Provider
            if (entity is not Tenant)
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

        public virtual async Task<PaginatedResult<TDestination>> GetPagedAsync<TDestination>(
            IQueryable<TDestination> query,
            int pageNumber,
            int pageSize,
            string? sortBy = null,
            bool isDescending = false)
        {
            // 1. Dynamic Sorting with Fallback
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                try
                {
                    var ordering = isDescending ? $"{sortBy} descending" : sortBy;
                    query = DynamicQueryableExtensions.OrderBy(query, ordering);
                }
                catch (Exception) // Catch if property doesn't exist
                {
                    query = query.OrderBy("Id");
                }
            }
            else
            {
                query = query.OrderBy("Id");
            }

            var count = await query.CountAsync();

            // Sanitize inputs
            var page = pageNumber < 1 ? 1 : pageNumber;
            var size = pageSize < 1 ? 10 : pageSize;

            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PaginatedResult<TDestination>(items, count, page, size);
        }
    }
}
