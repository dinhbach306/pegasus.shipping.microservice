# Entity Audit Fields

## Overview

All entities in the system inherit from the `Entity` base class which provides automatic audit tracking:

- **CreatedAt** - Timestamp when entity was created
- **UpdatedAt** - Timestamp when entity was last modified
- **IsActive** - Soft delete flag

## Entity Base Class

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    public void MarkAsUpdated();
    public void Deactivate();  // Soft delete
    public void Activate();    // Restore
}
```

## Automatic Timestamp Management

The system uses `AuditableEntityInterceptor` to automatically set timestamps:

- **On Create**: `CreatedAt` is set to `DateTime.UtcNow`
- **On Update**: `UpdatedAt` is automatically set to `DateTime.UtcNow`

### How It Works

```csharp
// EF Core SaveChanges interceptor
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    // Automatically called before saving changes
    // Sets UpdatedAt for modified entities
}
```

## Usage Examples

### 1. Creating a New Entity

```csharp
var shipment = new Shipment("TRACK123");
await context.Shipments.AddAsync(shipment);
await context.SaveChangesAsync();

// shipment.CreatedAt is automatically set to DateTime.UtcNow
// shipment.UpdatedAt is null
// shipment.IsActive is true
```

### 2. Updating an Entity

```csharp
var shipment = await context.Shipments.FindAsync(id);
shipment.UpdateStatus("Delivered");
await context.SaveChangesAsync();

// shipment.UpdatedAt is automatically set to DateTime.UtcNow by interceptor
```

**Or manually:**

```csharp
var shipment = await context.Shipments.FindAsync(id);
shipment.UpdateStatus("Delivered");
shipment.MarkAsUpdated(); // Explicitly set UpdatedAt
await context.SaveChangesAsync();
```

### 3. Soft Delete (Deactivate)

```csharp
var shipment = await context.Shipments.FindAsync(id);
shipment.Deactivate();
await context.SaveChangesAsync();

// shipment.IsActive is now false
// shipment.UpdatedAt is set to DateTime.UtcNow
```

### 4. Restore (Activate)

```csharp
var shipment = await context.Shipments.FindAsync(id);
shipment.Activate();
await context.SaveChangesAsync();

// shipment.IsActive is now true
// shipment.UpdatedAt is set to DateTime.UtcNow
```

### 5. Query Active Entities

```csharp
// Get only active shipments
var activeShipments = await context.Shipments
    .Where(s => s.IsActive)
    .ToListAsync();

// Get all shipments including soft-deleted
var allShipments = await context.Shipments
    .IgnoreQueryFilters() // If using global query filter
    .ToListAsync();
```

## Database Schema

All entity tables include these columns:

```sql
CREATE TABLE Shipments (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TrackingNumber NVARCHAR(64) NOT NULL,
    Status NVARCHAR(64) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    INDEX IX_Shipments_IsActive (IsActive),
    INDEX IX_Shipments_TrackingNumber (TrackingNumber)
);
```

## API Response Example

When entities are returned via API, audit fields are included:

```json
{
  "shipment": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "trackingNumber": "TRACK123",
    "status": "Delivered",
    "createdAt": "2026-01-15T10:30:00Z",
    "updatedAt": "2026-01-15T15:45:00Z",
    "isActive": true
  }
}
```

## Best Practices

### 1. Always Use MarkAsUpdated()

When manually updating entity properties outside of EF Core tracked changes:

```csharp
public void UpdateStatus(string status)
{
    Status = status;
    MarkAsUpdated(); // Important!
}
```

### 2. Prefer Soft Delete Over Hard Delete

```csharp
// ✅ Good - Soft delete
shipment.Deactivate();
await context.SaveChangesAsync();

// ❌ Bad - Hard delete (permanent)
context.Shipments.Remove(shipment);
await context.SaveChangesAsync();
```

### 3. Filter Inactive Entities by Default

Configure global query filter in DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Shipment>()
        .HasQueryFilter(s => s.IsActive);
}
```

Then queries automatically exclude inactive entities:

```csharp
// Only returns active shipments
var shipments = await context.Shipments.ToListAsync();

// To include inactive entities
var all = await context.Shipments
    .IgnoreQueryFilters()
    .ToListAsync();
```

### 4. Use UTC Timestamps

Always use `DateTime.UtcNow` instead of `DateTime.Now`:

```csharp
✅ public DateTime CreatedAt { get; } = DateTime.UtcNow;
❌ public DateTime CreatedAt { get; } = DateTime.Now;
```

### 5. Index IsActive for Performance

```csharp
builder.HasIndex(e => e.IsActive);
```

This improves query performance when filtering by active status.

## Migration Example

When creating a migration for existing entities, add audit fields:

```bash
dotnet ef migrations add AddAuditFields --project src/Services/Shipping/Shipping.Infrastructure
```

Generated migration:

```csharp
public partial class AddAuditFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAt",
            table: "Shipments",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "GETUTCDATE()");

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "Shipments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Shipments",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateIndex(
            name: "IX_Shipments_IsActive",
            table: "Shipments",
            column: "IsActive");
    }
}
```

## Querying Examples

### Get Recently Created Entities

```csharp
var recentShipments = await context.Shipments
    .Where(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-7))
    .OrderByDescending(s => s.CreatedAt)
    .ToListAsync();
```

### Get Recently Updated Entities

```csharp
var recentlyUpdated = await context.Shipments
    .Where(s => s.UpdatedAt != null && s.UpdatedAt >= DateTime.UtcNow.AddHours(-24))
    .OrderByDescending(s => s.UpdatedAt)
    .ToListAsync();
```

### Get Inactive Entities

```csharp
var inactiveShipments = await context.Shipments
    .IgnoreQueryFilters()
    .Where(s => !s.IsActive)
    .ToListAsync();
```

## Testing

Example unit test:

```csharp
[Fact]
public async Task UpdateStatus_ShouldSetUpdatedAt()
{
    // Arrange
    var shipment = new Shipment("TRACK123");
    await context.Shipments.AddAsync(shipment);
    await context.SaveChangesAsync();

    var originalCreatedAt = shipment.CreatedAt;
    await Task.Delay(100); // Ensure time difference

    // Act
    shipment.UpdateStatus("Delivered");
    await context.SaveChangesAsync();

    // Assert
    Assert.NotNull(shipment.UpdatedAt);
    Assert.True(shipment.UpdatedAt > originalCreatedAt);
    Assert.Equal(originalCreatedAt, shipment.CreatedAt); // CreatedAt unchanged
}

[Fact]
public void Deactivate_ShouldSetIsActiveFalse()
{
    // Arrange
    var shipment = new Shipment("TRACK123");

    // Act
    shipment.Deactivate();

    // Assert
    Assert.False(shipment.IsActive);
    Assert.NotNull(shipment.UpdatedAt);
}
```

## Troubleshooting

### UpdatedAt not being set automatically

**Problem**: `UpdatedAt` remains null after updates.

**Solution**: Ensure `AuditableEntityInterceptor` is registered in DbContext:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new AuditableEntityInterceptor());
}
```

### CreatedAt wrong timezone

**Problem**: `CreatedAt` shows local time instead of UTC.

**Solution**: Always use `DateTime.UtcNow`:

```csharp
✅ CreatedAt = DateTime.UtcNow;
❌ CreatedAt = DateTime.Now;
```

### Soft deleted entities still appearing

**Problem**: Deactivated entities still show up in queries.

**Solution**: Add global query filter:

```csharp
modelBuilder.Entity<Shipment>()
    .HasQueryFilter(s => s.IsActive);
```

## References

- `src/BuildingBlocks/SharedKernel/Entity.cs` - Base entity class
- `src/BuildingBlocks/SharedKernel/AuditableEntityInterceptor.cs` - EF Core interceptor
- `src/Services/Shipping/Shipping.Infrastructure/ShippingDbContext.cs` - Example DbContext configuration
