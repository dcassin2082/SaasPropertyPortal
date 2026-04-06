using PropertyPortal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Domain.Entities
{
    public class ResidentNote : BaseEntity
    {
        public Guid ResidentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Resident Resident { get; set; } = null!;
    }
}
