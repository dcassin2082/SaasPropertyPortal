using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Appliicants
{
    public record ApplicantResponse(
        Guid PropertyId,
        Guid UnitId,
        string FirstName,
        string LastName,
        string Email,
        string? CurrentStreet,
        string? CurrentUnitNumber,
        string? CurrentCity,
        string? CurrentState,
        string? CurrentZipCode,
        int? CreditScore
    );
}
