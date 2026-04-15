using PropertyPortal.Domain.Common;

namespace PropertyPortal.Domain.Entities
{
    public class Applicant : BaseEntity
    {
        public Guid PropertyId { get; set; }
        public Guid UnitId { get; set; }
        public virtual Property Property { get; set; } // Add this! EF Core handles the rest.
        public virtual Unit Unit { get; set; } // Add this! EF Core handles the rest.
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? CreditScore { get; set; }
        public string? Status { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public string? CurrentStreet { get; set; }
        public string? CurrentCity { get; set; }
        public string? CurrentUnitNumber { get; set; }
        public string? CurrentState { get; set; }
        public string? CurrentZipCode { get; set; }
    }
}
