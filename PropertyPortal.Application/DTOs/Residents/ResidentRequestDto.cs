using MediatR.Wrappers;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.DTOs.Residents
{
    public record ResidentRequestDto(
        Guid TenantId,
        Guid PropertyId,
        Guid UnitId,
        Guid ApplicantId,
        string FirstName,
        string LastName,
        string? Email,
        string? Phone,
        DateOnly LeaseStartDate,
        DateOnly LeaseEndDate,
        decimal RentAmount
    );
}

