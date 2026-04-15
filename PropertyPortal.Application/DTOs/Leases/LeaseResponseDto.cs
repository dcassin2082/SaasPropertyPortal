using PropertyPortal.Application.DTOs.Appliicants;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.DTOs.Leases
{
    public class LeaseListResponseDto
    {
        public IEnumerable<LeaseResponseDto> Applicants { get; set; }
        public int TotalCount { get; set; }
    }
    public class LeaseResponseDto
    {
        public Guid Id { get; set; }

        public Guid ResidentId { get; set; }

        public Guid PropertyId { get; set; }

        public Guid UnitId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public decimal MonthlyRent { get; set; }

        public decimal DepositAmount { get; set; }

        public string Status { get; set; } = null!;

        public decimal TotalPayments { get; set; }

        public string PropertyName { get; set; } = null!;

        public string? UnitNumber { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public decimal TotalDeposits { get; set; }

        public decimal TotalMonthlyRent { get; set; }
    }
}
