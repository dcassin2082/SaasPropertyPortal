using PropertyPortal.Domain.Common;
using PropertyPortal.Domain.Core.Interfaces;
using System.Text.Json.Serialization;

namespace PropertyPortal.Domain.Entities
{
    public partial class Resident : BaseEntity //, ILocatable
    {
        public Guid PropertyId { get; set; }

        public Guid? UnitId { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        //public DateOnly LeaseStartDate { get; set; }

        //public DateOnly LeaseEndDate { get; set; }

        //public decimal RentAmount { get; set; }

        public virtual Property? Property { get; set; } 

        public virtual Unit? Unit { get; set; }

        public virtual ICollection<Lease> Leases { get; set; } = new List<Lease>();

        //// ILocatable fields - remember this is for searching (Name = (FirstName + LastName), Description are not really needed on Residents table 
        //public Address Address { get; set; } = null!; // EF will flatten this into Property_Street, etc.
        //public string Name { get; set; } = null!;
        //public string? Description { get; set; }

        public string? Status { get; set; }
    }
}
