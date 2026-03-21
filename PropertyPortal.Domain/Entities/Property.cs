using PropertyPortal.Domain.Common;

namespace PropertyPortal.Domain.Entities;

public partial class Property : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? PropertyType { get; set; }

    public virtual Tenant? Tenant { get; set; } = null!;

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
}
