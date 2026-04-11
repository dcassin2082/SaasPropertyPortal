using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Appliicants
{
    public record CreateApplicantRequest(
        Guid TenantId,
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
    /*
     * {
     * 
  "tenandId": "45BE7686-5448-49B5-8526-29B05E0ABB9E",
  "propertyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "unitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "currentStreet": "string",
  "currentUnitNumber": "string",
  "currentCity": "string",
  "currentState": "string",
  "currentZipCode": "string",
  "creditScore": 0
}*/
}
