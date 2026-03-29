using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Infrastructure.Data;
using PropertyPortal.Infrastructure.Repositories;
using Unit = PropertyPortal.Domain.Entities.Unit;

namespace PropertyPortal.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public UnitOfWork(ApplicationDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public IBaseRepository<User> Users
        {
            get
            {
                return new BaseRepository<User>(_context, _tenantProvider);
            }
        }

        public IBaseRepository<Tenant> Tenants => new BaseRepository<Tenant>(_context, _tenantProvider);
        public IBaseRepository<Property> Properties => new BaseRepository<Property>(_context, _tenantProvider);
        public IBaseRepository<Unit> Units => new BaseRepository<Unit>(_context, _tenantProvider);
        public IBaseRepository<Lease> Leases => new BaseRepository<Lease>(_context, _tenantProvider);
        public IBaseRepository<Resident> Residents => new BaseRepository<Resident>(_context, _tenantProvider);
        // Initialize the join table repository
        public IBaseRepository<PropertyManager> PropertyManagers => new BaseRepository<PropertyManager>(_context, _tenantProvider);

        public async Task<int> CompleteAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Centralized conflict handling
                throw;
            }
        }

        public void Dispose() => _context.Dispose();
    }

}
