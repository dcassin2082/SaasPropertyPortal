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

    public virtual DbSet<Lease> Leases { get; set; }

    public virtual DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }
    /*  */

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // I also add this as the scaffolder did not originally
        base.OnModelCreating(modelBuilder);

        // This applies the filter to EVERY entity that inherits from BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Equivalent to: WHERE IsDeleted = 0 AND TenantId = currentTenantId
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    ConvertFilterExpression(entityType.ClrType)
                );
            }
        }
        modelBuilder.Entity<Lease>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Leases__3214EC07752D3833");

            entity.HasIndex(e => e.TenantId, "IX_Leases_Tenant_Active").HasFilter("([IsDeleted]=(0))");

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
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
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
