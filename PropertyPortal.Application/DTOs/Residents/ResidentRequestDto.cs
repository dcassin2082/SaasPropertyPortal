using MediatR.Wrappers;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.DTOs.Residents
{
    public record ResidentRequestDto(
        Guid PropertyId,
        Guid UnitId,
        string FirstName,
        string LastName,
        string? Email,
        string? Phone,
        DateOnly LeaseStartDate,
        DateOnly LeaseEndDate,
        decimal RentAmount,
        Address Address
    );
}

