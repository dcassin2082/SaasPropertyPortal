using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Application.DTOs.Units
{
    public class UnitResponseDto
    {
        public Guid Id { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public decimal Rent { get; set; }
        public string? PropertyName { get; set; }
        public Guid PropertyId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
