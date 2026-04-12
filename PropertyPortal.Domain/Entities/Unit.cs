using PropertyPortal.Domain.Core.Interfaces;
using PropertyPortal.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyPortal.Domain.Entities;

public partial class Unit : BaseEntity
{
    public Guid PropertyId { get; set; }

    public string? UnitNumber { get; set; } = null!;

    public int? Bedrooms { get; set; }

    public int? Bathrooms { get; set; }

    public decimal Rent { get; set; }

    public virtual ICollection<Lease> Leases { get; set; } = new List<Lease>();

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual ICollection<Resident> Residents { get; set; } = new List<Resident>();

    public virtual Property Property { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;

    //// from ILocatable interface
    ////public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? UnitType { get; set; } = null!;
    public string? Status { get; set; } = null!;

}
