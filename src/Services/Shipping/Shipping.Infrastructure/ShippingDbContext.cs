using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Shipping.Domain;

namespace Shipping.Infrastructure;

public sealed class ShippingDbContext(DbContextOptions<ShippingDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new AuditableEntityInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(builder =>
        {
            builder.ToTable("Shipments");
            builder.HasKey(shipment => shipment.Id);
            builder.Property(shipment => shipment.TrackingNumber).HasMaxLength(64).IsRequired();
            builder.Property(shipment => shipment.Status).HasMaxLength(64).IsRequired();
            builder.HasIndex(shipment => shipment.TrackingNumber).IsUnique();
            
            // Audit fields from Entity base class
            builder.Property(shipment => shipment.CreatedAt).IsRequired();
            builder.Property(shipment => shipment.UpdatedAt);
            builder.Property(shipment => shipment.IsActive).IsRequired().HasDefaultValue(true);
            builder.HasIndex(shipment => shipment.IsActive);
        });
    }
}

