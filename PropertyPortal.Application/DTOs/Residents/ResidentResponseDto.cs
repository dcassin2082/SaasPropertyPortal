using PropertyPortal.Domain.Entities;
using PropertyPortal.Domain.Enums;

namespace PropertyPortal.Application.DTOs.Residents
{
    public record ResidentResponseDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    Guid? UnitId,
    string UnitNumber,
    string FullName,        // Pre-joined: "John Doe"
    string? Email,
    string? Phone,
    DateOnly LeaseStartDate,
    DateOnly LeaseEndDate,
    decimal RentAmount,
    bool IsDeleted,
    string DisplayAddress,  // Pre-formatted: "123 Main St, Gilbert, AZ"
    string? Status
);
}
