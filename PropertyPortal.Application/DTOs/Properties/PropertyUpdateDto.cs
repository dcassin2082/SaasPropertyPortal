using PropertyPortal.Domain.Core.Interfaces;
using PropertyPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PropertyPortal.Application.DTOs.Properties
{
    public class PropertyUpdateDto 
    {
        public Guid Id { get; set; } = Guid.NewGuid();

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

        //[Timestamp] // Critical for EF Core to treat this as the concurrency token
        public byte[] RowVersion { get; set; } = null!;
    }
}
