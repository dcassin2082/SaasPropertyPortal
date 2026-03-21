using PropertyPortal.Domain.Common;

namespace PropertyPortal.Domain.Entities;

public partial class User : BaseEntity
{
    public string Email { get; set; } = null!;

    public string? NormalizedEmail { get; set; }

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual ICollection<Lease> Leases { get; set; } = new List<Lease>();

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual Tenant Tenant { get; set; } = null!;
}
