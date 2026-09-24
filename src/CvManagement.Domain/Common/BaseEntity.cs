namespace CvManagement.Domain.Common;

/// <summary>
/// Base class for all entities. Provides:
/// - Unique ID (GUID)
/// - Audit timestamps
/// - Optimistic locking via Version field
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optimistic locking version (PostgreSQL xmin).
    /// EF Core automatically maps this to `xmin` column.
    /// </summary>
    public uint Version { get; set; }
}