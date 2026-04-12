using PropertyPortal.Domain.Common;
using PropertyPortal.Domain.Core.Interfaces;

namespace PropertyPortal.Domain.Entities;

public partial class Property : BaseEntity //, ILocatable
{
    public string? PropertyType { get; set; }

    public virtual Tenant? Tenant { get; set; } = null!;
    public string? Address1 { get; set; } = null!;
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; } 

    //// from ILocatable interface
    public string Name { get; set; } = null!;
    //public required Address Address { get; set; } // EF will flatten this into Property_Street, etc.
    //public string? Description { get; set; } 

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();

    public virtual ICollection<Resident> Residents { get; set; } = new List<Resident>();
}
