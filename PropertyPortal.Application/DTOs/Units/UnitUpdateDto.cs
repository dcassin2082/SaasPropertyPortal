using System.ComponentModel.DataAnnotations;

namespace PropertyPortal.Application.DTOs.Units
{
    public class UnitUpdateDto
    {
        [Required]
        public string UnitNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public decimal Rent { get; set; }
    }
}
