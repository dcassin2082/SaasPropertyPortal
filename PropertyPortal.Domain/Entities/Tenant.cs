using PropertyPortal.Domain.Common;

namespace PropertyPortal.Domain.Entities;

public partial class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual ICollection<Lease> Leases { get; set; } = new List<Lease>();

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
