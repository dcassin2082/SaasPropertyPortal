using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Appliicants
{
    public record ApplicantResponseDto
    {
        public Guid Id { get; init; }
        public Guid PropertyId { get; init; }
        public Guid UnitId { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? Email { get; init; } = string.Empty;
        public string? Phone { get; init; } = string.Empty;
        public string? PropertyName { get; init; }
        public string? UnitNumber { get; init; }
        public string? CurrentStreet { get; init; }
        public string? CurrentUnitNumber { get; init; }
        public string? CurrentCity { get; init; }
        public string? CurrentState { get; init; }
        public string? CurrentZipCode { get; init; }
        public int? CreditScore { get; init; }
        public DateTime? ApplicationDate { get; set; }
        public string? Status { get; set; }

    }
}
