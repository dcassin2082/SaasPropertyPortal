using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Residents
{
    public record MoveInRequest
    (
        Guid ResidentId,
        Guid UnitId,
        DateOnly StartDate,
        DateOnly EndDate,
        decimal DepositAmount
    );
}
