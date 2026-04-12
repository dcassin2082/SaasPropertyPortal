using PropertyPortal.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PropertyPortal.Domain.Entities;

public partial class Lease : BaseEntity
{
    public Guid ResidentId { get; set; }

    public Guid PropertyId { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    public Guid UnitId { get; set; }

    public Guid UserId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal MonthlyRent { get; set; }

    public decimal DepositAmount { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Resident Resident { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;

    public virtual User User { get; set; } = null!;
    public bool IsCurrentlyActive =>
        Status == "Active" &&
        StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) &&
        EndDate >= DateOnly.FromDateTime(DateTime.UtcNow);
}
