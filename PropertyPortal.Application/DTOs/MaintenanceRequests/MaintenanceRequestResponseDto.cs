using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.MaintenanceRequests
{
    public record MaintenanceRequestResponseDto
    {
        public Guid Id { get; init; }
        public string Subject { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Status { get; init; } = "Open"; // Open, In-Progress, Resolved, Cancelled
        public string Priority { get; init; } = "Medium"; // Low, Medium, High, Emergency
        public string PropertyName { get; init; } = string.Empty;
        public string UnitNumber { get; init; } = string.Empty;
        public string ResidentName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
