namespace SharedKernel;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    /// <summary>
    /// Mark entity as updated (sets UpdatedAt timestamp)
    /// </summary>
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete entity by setting IsActive to false
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Restore soft-deleted entity
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}

