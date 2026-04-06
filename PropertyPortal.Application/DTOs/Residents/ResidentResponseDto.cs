using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.DTOs.Residents
{
    public record ResidentResponseDto(
        Guid Id,
        Guid PropertyId,
        string PropertyName, // Flattened for the UI
        Guid UnitId,
        string FirstName,
        string LastName,
        string Name,        // From ILocatable
        string? Email,
        string? Phone,
        DateOnly LeaseStartDate,
        DateOnly LeaseEndDate,
        decimal RentAmount,
        Address Address,
        string UnitNumber
    );
}
