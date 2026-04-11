using PropertyPortal.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyPortal.Domain.Entities;

public partial class Payment : BaseEntity
{
    public Guid LeaseId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? PaymentMethod { get; set; }

    public string? ExternalReference { get; set; }

    public string Status { get; set; } = null!;

    public virtual Lease Lease { get; set; } = null!;

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;
}
