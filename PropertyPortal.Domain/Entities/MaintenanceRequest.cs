using PropertyPortal.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyPortal.Domain.Entities;

public partial class MaintenanceRequest : BaseEntity
{
    public Guid UnitId { get; set; }

    public Guid CreatedByUserId { get; set; }
    public Guid PropertyId { get; set; }

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Priority { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;

    public string? Subject { get; set; }
}
