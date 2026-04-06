using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PropertyPortal.Application.DTOs.Units
{
    public class UnitBulkCreateDto
    {
        [Required]
        public Guid PropertyId { get; set; }

        [Required]
        [Range(1, 50)] // Limit bulk creation to prevent server abuse
        public int Count { get; set; }

        [Required]
        public int StartingNumber { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal BaseRent { get; set; }

        public string? UnitType { get; set; } // e.g., "Studio", "2BR"
    }
}
