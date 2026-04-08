using PropertyPortal.Application.DTOs.Units;
using PropertyPortal.Domain.Common;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.DTOs.Properties
{
    public class PropertyResponseDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public required Address Address { get; set; }

        public string? PropertyType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid CreatedBy { get; set; }

        public bool IsDeleted { get; set; }

        // Calculated Fields (mapping performed in Program.cs ... TypeAdapterConfig(Property, PropertyResponseDto> ...
        public int UnitCount { get; set; }
        
        public int ResidentCount { get; set; }

        public decimal TotalMonthlyRent { get; set; }

        public byte[] RowVersion { get; set; } = null!;

        // ADD THIS LINE:
        // This allows the "Include(p => p.Units)" data to actually reach the frontend
        public List<UnitResponseDto> Units { get; set; } = new();
    }
}
