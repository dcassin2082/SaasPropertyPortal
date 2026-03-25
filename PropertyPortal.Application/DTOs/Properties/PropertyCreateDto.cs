using PropertyPortal.Domain.Core.Interfaces;
using PropertyPortal.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace PropertyPortal.Application.DTOs.Properties
{
    public class PropertyCreateDto : ILocatable
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public required Address Address { get; set; }

        [StringLength(50)]
        public string? PropertyType { get; set; }
    }
}
