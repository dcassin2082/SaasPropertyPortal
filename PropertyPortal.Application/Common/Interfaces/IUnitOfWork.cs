using Application.Common.Interfaces;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Access points for your repositories
        IBaseRepository<Property> Properties { get; }
        IBaseRepository<Unit> Units { get; }
        IBaseRepository<Lease> Leases { get; }
        IBaseRepository<User> Users { get; }
        IBaseRepository<Tenant> Tenants { get; }
        IBaseRepository<Resident> Residents { get; }
        IBaseRepository<PropertyManager> PropertyManagers { get; }
        // The "Big Button" to save everything in one transaction
        Task<int> CompleteAsync();
    }

}
