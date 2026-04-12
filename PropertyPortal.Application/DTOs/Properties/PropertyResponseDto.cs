using PropertyPortal.Application.DTOs.Units;

namespace PropertyPortal.Application.DTOs.Properties
{
    public class PropertyResponseDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = null!;

        public string? Address1 { get; set; } 

        public string? Address2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? ZipCode { get; set; }

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
