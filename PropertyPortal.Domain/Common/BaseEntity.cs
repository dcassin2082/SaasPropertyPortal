using System.ComponentModel.DataAnnotations;

namespace PropertyPortal.Domain.Common
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        [Timestamp] // Critical for EF Core to treat this as the concurrency token
        public byte[] RowVersion { get; set; } = null!;
        /* 3. Handling the UI (The "Hidden" Requirement)
                For Database First to work with ROWVERSION correctly, your Edit forms must include the version number in a hidden field. 
                If the user loads the page, the RowVersion comes with it. When they hit "Save," that same version number is sent back.
                If you don't include it in the form, EF Core will see a null or empty RowVersion and think it's a brand new record or a corrupted update.
        */
    }
}
