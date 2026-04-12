using PropertyPortal.Domain.Core.Interfaces;
using PropertyPortal.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace PropertyPortal.Application.DTOs.Properties
{
    public class PropertyCreateDto // : ILocatable
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        //public required Address Address { get; set; }

        public string? Address1 { get; set; } = null!;
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        [StringLength(50)]
        public string? PropertyType { get; set; }

    }
}
