using PropertyPortal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPortal.Domain.Entities
{
    public class PropertyManager : BaseEntity
    {
        // Composite Key / Foreign Keys
        public Guid PropertyId { get; set; }
        public Guid UserId { get; set; }

        // Navigation Properties
        public virtual Property Property { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
