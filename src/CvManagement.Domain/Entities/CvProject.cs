using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Snapshot of a project selected for a specific CV.
/// Ordered by DisplayOrder.
/// </summary>
public class CvProject : BaseEntity
{
    public Guid CvId { get; set; }
    public Cv Cv { get; set; } = null!;
    
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Snapshot of project data at CV generation time.
    /// Prevents live project edits from changing historical CVs.
    /// JSON-serialized.
    /// </summary>
    public string? ProjectSnapshotJson { get; set; }
}