using PropertyPortal.Domain.Common;
using PropertyPortal.Domain.Core.Interfaces;

namespace PropertyPortal.Domain.Entities
{
    public partial class Resident : BaseEntity, ILocatable
    {
        public Guid PropertyId { get; set; }

        public Guid UnitId { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public DateOnly LeaseStartDate { get; set; }

        public DateOnly LeaseEndDate { get; set; }

        public decimal RentAmount { get; set; }

        public virtual Property Property { get; set; } = null!;

        public virtual Unit Unit { get; set; } = null!;

        // ILocatable fields - remember this is for searching (Name = (FirstName + LastName), Description are not really needed on Residents table 
        public required Address Address { get; set; } // EF will flatten this into Property_Street, etc.
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        // Map existing fields to the interface requirements
    }
}
