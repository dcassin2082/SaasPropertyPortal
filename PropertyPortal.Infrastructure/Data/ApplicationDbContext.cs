using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Domain.Common;
using System.Linq.Expressions;

namespace PropertyPortal.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public virtual DbSet<Applicant> Applicants { get; set; }

    public virtual DbSet<Lease> Leases { get; set; }

    public virtual DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Resident> Residents { get; set; }

    public virtual DbSet<ResidentNote> ResidentNotes { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Force the TenantId on creation
                    if (entry.Entity.TenantId == Guid.Empty && tenantId != Guid.Empty)
                    {
                        entry.Entity.TenantId = tenantId;
                    }
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    // Ensure the TenantId doesn't get "wiped" during an update
                    entry.Property(x => x.TenantId).IsModified = false;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // move the connection string out of source code ...
        ////optionsBuilder.UseSqlServer("server=LAPTOP-ME5NKIM9\\SQLEXPRESS;database=PropertyPortal;trusted_connection=true;multipleactiveresultsets=true;TrustServerCertificate=true;");

        /* now that we've moved the connection string to appsettings.development.json
         * we needed to modify this to detect the environment */
        //if (!optionsBuilder.IsConfigured)
        //{
        //    var builder = new ConfigurationBuilder();
        //    builder.AddJsonFile("appsettings.Development.json", optional: false);
        //    var config = builder.Build();
        //    var cs = config.GetConnectionString("LocalPropertyPortalConnection");
        //    optionsBuilder.UseSqlServer(cs);
        //}

        // Detect the environment (defaults to Production if not set)
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Ensures the file is found
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true) // Loads Development/Production
            .AddEnvironmentVariables(); // Good practice for cloud deployments

        var config = builder.Build();
        var cs = config.GetConnectionString("LocalPropertyPortalConnection");
        optionsBuilder.UseSqlServer(cs);
    }

    private LambdaExpression ConvertFilterExpression(Type type)
    {
        var parameter = Expression.Parameter(type, "e");
        var isDeletedCheck = Expression.Equal(Expression.Property(parameter, "IsDeleted"), Expression.Constant(false));

        // If you also want to automate Tenant Isolation here:
        var tenantCheck = Expression.Equal(Expression.Property(parameter, "TenantId"), Expression.Constant(_tenantProvider.GetTenantId()));
        var combined = Expression.AndAlso(isDeletedCheck, tenantCheck);

        return Expression.Lambda(combined, parameter);
    }
    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.Entity<Applicant>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK__Applican__3214EC07403A5D0A");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.ApplicationDate).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.CurrentCity).HasMaxLength(100);
    //        entity.Property(e => e.CurrentState).HasMaxLength(50);
    //        entity.Property(e => e.CurrentStreet).HasMaxLength(255);
    //        entity.Property(e => e.CurrentUnitNumber).HasMaxLength(50);
    //        entity.Property(e => e.CurrentZipCode).HasMaxLength(20);
    //        entity.Property(e => e.Email).HasMaxLength(255);
    //        entity.Property(e => e.FirstName).HasMaxLength(100);
    //        entity.Property(e => e.LastName).HasMaxLength(100);
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status).HasMaxLength(50);
    //    });

    //    modelBuilder.Entity<Lease>(entity =>
    //    {
    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
    //        entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status)
    //            .HasMaxLength(50)
    //            .HasDefaultValue("Draft");

    //        entity.HasOne(d => d.Resident).WithMany(p => p.Leases)
    //            .HasForeignKey(d => d.ResidentId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Leases_Residents");

    //        entity.HasOne(d => d.Tenant).WithMany(p => p.Leases)
    //            .HasForeignKey(d => d.TenantId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Leases_Tenants");

    //        entity.HasOne(d => d.Unit).WithMany(p => p.Leases)
    //            .HasForeignKey(d => d.UnitId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Leases_Units");

    //        entity.HasOne(d => d.User).WithMany(p => p.Leases)
    //            .HasForeignKey(d => d.UserId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Leases_Users");
    //    });

    //    modelBuilder.Entity<MaintenanceRequest>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK__Maintena__3214EC0710574D96");

    //        entity.HasIndex(e => e.TenantId, "IX_Maintenance_Tenant_Active").HasFilter("([IsDeleted]=(0))");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.Priority).HasMaxLength(50);
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status)
    //            .HasMaxLength(50)
    //            .HasDefaultValue("Open");

    //        entity.HasOne(d => d.CreatedByUser).WithMany(p => p.MaintenanceRequests)
    //            .HasForeignKey(d => d.CreatedByUserId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Maintenance_Users");

    //        entity.HasOne(d => d.Tenant).WithMany(p => p.MaintenanceRequests)
    //            .HasForeignKey(d => d.TenantId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Maintenance_Tenants");

    //        entity.HasOne(d => d.Unit).WithMany(p => p.MaintenanceRequests)
    //            .HasForeignKey(d => d.UnitId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Maintenance_Units");
    //    });

    //    modelBuilder.Entity<Payment>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC0778350361");

    //        entity.HasIndex(e => e.TenantId, "IX_Payments_Tenant_Active").HasFilter("([IsDeleted]=(0))");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.ExternalReference).HasMaxLength(100);
    //        entity.Property(e => e.PaymentMethod).HasMaxLength(50);
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status).HasMaxLength(50);

    //        entity.HasOne(d => d.Lease).WithMany(p => p.Payments)
    //            .HasForeignKey(d => d.LeaseId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Payments_Leases");

    //        entity.HasOne(d => d.Tenant).WithMany(p => p.Payments)
    //            .HasForeignKey(d => d.TenantId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Payments_Tenants");
    //    });

    //    modelBuilder.Entity<Property>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK__Properti__3214EC07E7BC354F");

    //        entity.HasIndex(e => e.TenantId, "IX_Properties_Tenant_Active").HasFilter("([IsDeleted]=(0))");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.AddressCity)
    //            .HasMaxLength(100)
    //            .HasDefaultValue("")
    //            .HasColumnName("Address_City");
    //        entity.Property(e => e.AddressState)
    //            .HasMaxLength(50)
    //            .HasDefaultValue("")
    //            .HasColumnName("Address_State");
    //        entity.Property(e => e.AddressStreet)
    //            .HasMaxLength(200)
    //            .HasDefaultValue("")
    //            .HasColumnName("Address_Street");
    //        entity.Property(e => e.AddressUnitNumber)
    //            .HasMaxLength(50)
    //            .HasColumnName("Address_UnitNumber");
    //        entity.Property(e => e.AddressZipCode)
    //            .HasMaxLength(20)
    //            .HasDefaultValue("")
    //            .HasColumnName("Address_ZipCode");
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.Description).HasMaxLength(500);
    //        entity.Property(e => e.Name).HasMaxLength(200);
    //        entity.Property(e => e.PropertyType).HasMaxLength(50);
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();

    //        entity.HasOne(d => d.Tenant).WithMany(p => p.Properties)
    //            .HasForeignKey(d => d.TenantId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Properties_Tenants");
    //    });

    //    modelBuilder.Entity<PropertyManager>(entity =>
    //    {
    //        entity.HasKey(e => new { e.PropertyId, e.UserId });

    //        entity.HasOne(d => d.Property).WithMany(p => p.PropertyManagers)
    //            .HasForeignKey(d => d.PropertyId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_PropertyManagers_Properties");

    //        entity.HasOne(d => d.User).WithMany(p => p.PropertyManagers)
    //            .HasForeignKey(d => d.UserId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_PropertyManagers_Users");
    //    });

    //    modelBuilder.Entity<Resident>(entity =>
    //    {
    //        entity.HasIndex(e => new { e.TenantId, e.PropertyId }, "IX_Residents_Tenant_Property");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.AddressCity)
    //            .HasMaxLength(100)
    //            .HasColumnName("Address_City");
    //        entity.Property(e => e.AddressState)
    //            .HasMaxLength(50)
    //            .HasColumnName("Address_State");
    //        entity.Property(e => e.AddressStreet)
    //            .HasMaxLength(200)
    //            .HasColumnName("Address_Street");
    //        entity.Property(e => e.AddressUnitNumber)
    //            .HasMaxLength(50)
    //            .HasColumnName("Address_UnitNumber");
    //        entity.Property(e => e.AddressZipCode)
    //            .HasMaxLength(20)
    //            .HasColumnName("Address_ZipCode");
    //        entity.Property(e => e.Description).HasMaxLength(500);
    //        entity.Property(e => e.Email).HasMaxLength(255);
    //        entity.Property(e => e.FirstName).HasMaxLength(100);
    //        entity.Property(e => e.LastName).HasMaxLength(100);
    //        entity.Property(e => e.Name).HasMaxLength(200);
    //        entity.Property(e => e.Phone).HasMaxLength(50);
    //        entity.Property(e => e.RentAmount).HasColumnType("decimal(18, 2)");
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status).HasMaxLength(50);
    //    });

    //    modelBuilder.Entity<ResidentNote>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK_ResidentNote");

    //        entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
    //        entity.Property(e => e.Content).HasDefaultValue("");
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //    });

    //    modelBuilder.Entity<Tenant>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK__Tenants__3214EC07EC16F35F");

    //        entity.HasIndex(e => e.Id, "IX_Tenants_Active").HasFilter("([IsDeleted]=(0))");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.Name).HasMaxLength(200);
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status)
    //            .HasMaxLength(50)
    //            .HasDefaultValue("Active");
    //    });

    //    modelBuilder.Entity<Unit>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK__Units__3214EC07DB475BD7");

    //        entity.HasIndex(e => e.PropertyId, "IX_Units_Property_Active").HasFilter("([IsDeleted]=(0))");

    //        entity.HasIndex(e => e.TenantId, "IX_Units_Tenant_Active").HasFilter("([IsDeleted]=(0))");

    //        entity.HasIndex(e => new { e.PropertyId, e.UnitNumber }, "UIX_Units_Property_UnitNumber")
    //            .IsUnique()
    //            .HasFilter("([IsDeleted]=(0))");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.Description).HasMaxLength(500);
    //        entity.Property(e => e.Name).HasMaxLength(200);
    //        entity.Property(e => e.Rent).HasColumnType("decimal(18, 2)");
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status).HasMaxLength(50);
    //        entity.Property(e => e.UnitNumber).HasMaxLength(50);
    //        entity.Property(e => e.UnitType).HasMaxLength(50);

    //        entity.HasOne(d => d.Property).WithMany(p => p.Units)
    //            .HasForeignKey(d => d.PropertyId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Units_Properties");

    //        entity.HasOne(d => d.Tenant).WithMany(p => p.Units)
    //            .HasForeignKey(d => d.TenantId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Units_Tenants");
    //    });

    //    modelBuilder.Entity<User>(entity =>
    //    {
    //        entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07C0269351");

    //        entity.HasIndex(e => e.TenantId, "IX_Users_Tenant_Active").HasFilter("([IsDeleted]=(0))");

    //        entity.HasIndex(e => e.NormalizedEmail, "UX_Users_Email_Active")
    //            .IsUnique()
    //            .HasFilter("([IsDeleted]=(0))");

    //        entity.Property(e => e.Id).ValueGeneratedNever();
    //        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
    //        entity.Property(e => e.Email).HasMaxLength(255);
    //        entity.Property(e => e.NormalizedEmail)
    //            .HasMaxLength(255)
    //            .HasComputedColumnSql("(upper([Email]))", true);
    //        entity.Property(e => e.PasswordHash).HasMaxLength(500);
    //        entity.Property(e => e.Role).HasMaxLength(50);
    //        entity.Property(e => e.RowVersion)
    //            .IsRowVersion()
    //            .IsConcurrencyToken();
    //        entity.Property(e => e.Status)
    //            .HasMaxLength(50)
    //            .HasDefaultValue("Active");

    //        entity.HasOne(d => d.Tenant).WithMany(p => p.Users)
    //            .HasForeignKey(d => d.TenantId)
    //            .OnDelete(DeleteBehavior.ClientSetNull)
    //            .HasConstraintName("FK_Users_Tenants");
    //    });

    //    OnModelCreatingPartial(modelBuilder);
    //}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // I also add this as the scaffolder did not originally
        base.OnModelCreating(modelBuilder);

        // This applies the filter to EVERY entity that inherits from BaseEntity
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
            {
                // Force link the Tenant property to the TenantId column
                var tenantNav = entity.FindNavigation("Tenant");
                if (tenantNav != null)
                {
                    entity.GetForeignKeys()
                        .Where(fk => fk.PrincipalEntityType.ClrType == typeof(Tenant))
                        .ToList()
                        .ForEach(fk => entity.RemoveForeignKey(fk));

                    modelBuilder.Entity(entity.ClrType)
                        .HasOne("Tenant")
                        .WithMany()
                        .HasForeignKey("TenantId");
                }
            }
        }
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // 2. Global Query Filter
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(ConvertFilterExpression(entityType.ClrType));
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                //// 1. Existing Query Filter logic
                //modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                //    ConvertFilterExpression(entityType.ClrType)
                //);

                // 2. FIX: Explicitly link the 'Tenant' navigation property to 'TenantId'
                // This prevents the creation of 'TenantId1'
                var tenantNav = entityType.FindNavigation("Tenant");
                if (tenantNav != null)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .HasOne("Tenant")
                        .WithMany()
                        .HasForeignKey("TenantId") // Explicitly use the one from BaseEntity
                                                   //.IsRequired()
                        .OnDelete(DeleteBehavior.Restrict);
                }


            }
        }

        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.ToTable("Applicants", "dbo");

            entity.HasKey(e => e.Id)
                .HasName("PK__Applican__3214EC07403A5D0A");

            // Set mandatory properties
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);

            // Address lengths
            entity.Property(e => e.CurrentStreet).HasMaxLength(255);
            entity.Property(e => e.CurrentCity).HasMaxLength(100);
            entity.Property(e => e.CurrentUnitNumber).HasMaxLength(50);
            entity.Property(e => e.CurrentState).HasMaxLength(50);
            entity.Property(e => e.CurrentZipCode).HasMaxLength(20);

            // ApplicationDate Default Value
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("sysutcdatetime()");

            // Audit fields
            entity.Property(e => e.CreatedAt).IsRequired();

            // RowVersion / Concurrency token
            entity.Property(e => e.RowVersion)
                .IsRowVersion();

            entity.HasOne<Tenant>() // Or .HasOne(e => e.Tenant) if the property exists
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.NoAction);

            // Now you can add the index you were thinking of
            entity.HasIndex(e => e.TenantId, "IX_Applicants_TenantId");
        });

        modelBuilder.Entity<Lease>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Draft");

            entity.HasOne(d => d.Resident).WithMany(p => p.Leases)
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leases_Residents");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Leases)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leases_Tenants");

            entity.HasOne(d => d.Unit).WithMany(p => p.Leases)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leases_Units");

            entity.HasOne(d => d.User).WithMany(p => p.Leases)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leases_Users");
        });

        modelBuilder.Entity<MaintenanceRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3214EC0710574D96");

            entity.HasIndex(e => e.TenantId, "IX_Maintenance_Tenant_Active").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Open");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_Users");

            entity.HasOne(d => d.Tenant).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_Tenants");

            entity.HasOne(d => d.Unit).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_Units");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC0778350361");

            entity.HasIndex(e => e.TenantId, "IX_Payments_Tenant_Active").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ExternalReference).HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Lease).WithMany(p => p.Payments)
                .HasForeignKey(d => d.LeaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Leases");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Payments)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Tenants");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            // flattened address record
            entity.ComplexProperty(p => p.Address, a =>
            {
                // This maps C# 'Street' to SQL column 'Address_Street'
                a.Property(ad => ad.Street).HasMaxLength(200).HasColumnName("Address_Street");
                a.Property(ad => ad.UnitNumber).HasMaxLength(50).HasColumnName("Address_UnitNumber");
                a.Property(ad => ad.City).HasMaxLength(100).HasColumnName("Address_City");
                a.Property(ad => ad.State).HasMaxLength(50).HasColumnName("Address_State");
                a.Property(ad => ad.ZipCode).HasMaxLength(20).HasColumnName("Address_ZipCode");
            });
            // ADD THIS LINE:
            entity.HasQueryFilter(p => p.TenantId == _tenantProvider.GetTenantId() && !p.IsDeleted);

            entity.HasKey(e => e.Id).HasName("PK__Properti__3214EC07E7BC354F");

            entity.HasIndex(e => e.TenantId, "IX_Properties_Tenant_Active").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PropertyType).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Tenant).WithMany(p => p.Properties)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Properties_Tenants");
        });

        modelBuilder.Entity<PropertyManager>(entity =>
        {
            // Replace "PropertyManagers" with the EXACT name of the table in your SQL DB
            entity.ToTable("PropertyManagers");

            // 1. Define the composite primary key
            entity.HasKey(pm => new { pm.PropertyId, pm.UserId });

            // 2. Map relationships
            modelBuilder.Entity<PropertyManager>()
                 .HasOne(p => p.Property)
                 .WithMany()
                 .HasForeignKey(p => p.PropertyId)
                 .IsRequired(false);

            entity.HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // 3. Ensure Tenant Filter applies to the join table too
            // (Assuming your BaseEntity filter logic handles this automatically)
            entity.Ignore(pm => pm.Id);
        });

        modelBuilder.Entity<Resident>(entity =>
        {
            // Force EF to treat the Id as a Guid
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("uniqueidentifier")
            .HasColumnName("Id")
            .ValueGeneratedNever();

            // Also ensure the Foreign Keys are mapped correctly as Guids
            entity.Property(e => e.PropertyId).HasColumnType("uniqueidentifier");
            entity.Property(e => e.UnitId).HasColumnType("uniqueidentifier");
            entity.HasIndex(e => new { e.TenantId, e.PropertyId }, "IX_Residents_Tenant_Property");

            entity.Property(e => e.Id).ValueGeneratedNever();

            // flattened address record
            entity.ComplexProperty(p => p.Address, a =>
            {
                // This maps C# 'Street' to SQL column 'Address_Street'
                a.Property(ad => ad.Street).HasMaxLength(200).HasColumnName("Address_Street");
                a.Property(ad => ad.UnitNumber).HasMaxLength(50).HasColumnName("Address_UnitNumber");
                a.Property(ad => ad.City).HasMaxLength(100).HasColumnName("Address_City");
                a.Property(ad => ad.State).HasMaxLength(50).HasColumnName("Address_State");
                a.Property(ad => ad.ZipCode).HasMaxLength(20).HasColumnName("Address_ZipCode");
            });
            //entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.RentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            //entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            entity.HasOne(d => d.Property).WithMany(p => p.Residents)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Residents_Properties");

            entity.HasOne(d => d.Unit).WithMany(p => p.Residents)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Residents_Units");

            entity.HasMany(d => d.Leases)
                .WithOne(p => p.Resident) // Or .WithOne(p => p.Tenant) depending on your prop name
                .HasForeignKey(p => p.ResidentId);
        });

        modelBuilder.Entity<ResidentNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ResidentNote");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Content).HasDefaultValue("");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.Ignore(t => t.TenantId);
            entity.HasKey(e => e.Id).HasName("PK__Tenants__3214EC07EC16F35F");

            entity.HasIndex(e => e.Id, "IX_Tenants_Active").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Units__3214EC07DB475BD7");

            entity.HasIndex(e => e.PropertyId, "IX_Units_Property_Active").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.TenantId, "IX_Units_Tenant_Active").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            // flattened address record
            //entity.ComplexProperty(p => p.Address, a =>
            //{
            //    // This maps C# 'Street' to SQL column 'Address_Street'
            //    a.Property(ad => ad.Street).HasMaxLength(200).HasColumnName("Address_Street");
            //    a.Property(ad => ad.UnitNumber).HasMaxLength(50).HasColumnName("Address_UnitNumber");
            //    a.Property(ad => ad.City).HasMaxLength(100).HasColumnName("Address_City");
            //    a.Property(ad => ad.State).HasMaxLength(50).HasColumnName("Address_State");
            //    a.Property(ad => ad.ZipCode).HasMaxLength(20).HasColumnName("Address_ZipCode");
            //});
            //entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            //entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Rent).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UnitNumber).HasMaxLength(50);

            entity.HasOne(d => d.Property).WithMany(p => p.Units)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Units_Properties");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Units)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Units_Tenants");
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07C0269351");

            entity.HasIndex(e => e.TenantId, "IX_Users_Tenant_Active").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.NormalizedEmail, "UX_Users_Email_Active")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.NormalizedEmail)
                .HasMaxLength(255)
                .HasComputedColumnSql("(upper([Email]))", true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Users)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Tenants");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
