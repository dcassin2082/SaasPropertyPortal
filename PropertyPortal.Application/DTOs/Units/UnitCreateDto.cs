using System.ComponentModel.DataAnnotations;

namespace PropertyPortal.Application.DTOs.Units
{
    public class UnitCreateDto
    {
        [Required(ErrorMessage = "Unit number is required")]
        [MaxLength(50)]
        public string UnitNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, 10, ErrorMessage = "Bedrooms must be between 0 and 10")]
        public int Bedrooms { get; set; }

        [Range(0, 10)]
        public int Bathrooms { get; set; }
        
        [Range(0, 10000)]
        public decimal Rent { get; set; }

        [Required]
        public Guid PropertyId { get; set; }
    }
}
