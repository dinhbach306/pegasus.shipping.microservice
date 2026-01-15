using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new AuditableEntityInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.ToTable("UserProfiles");
            builder.HasKey(profile => profile.Id);
            builder.Property(profile => profile.Auth0UserId).HasMaxLength(128).IsRequired();
            builder.Property(profile => profile.Email).HasMaxLength(256).IsRequired();
            builder.Property(profile => profile.FullName).HasMaxLength(256);
            builder.Property(profile => profile.Role).HasMaxLength(50).IsRequired().HasDefaultValue("user");
            builder.HasIndex(profile => profile.Auth0UserId).IsUnique();
            builder.HasIndex(profile => profile.Role);
            
            // Audit fields from Entity base class
            builder.Property(profile => profile.CreatedAt).IsRequired();
            builder.Property(profile => profile.UpdatedAt);
            builder.Property(profile => profile.IsActive).IsRequired().HasDefaultValue(true);
            builder.HasIndex(profile => profile.IsActive);
        });
    }
}

