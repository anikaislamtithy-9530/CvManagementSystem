using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Technology tag attached to a project.
/// Used for tag cloud + CV project filtering.
/// </summary>
public class ProjectTag : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    /// <summary>Tag text (e.g., "React", "PostgreSQL").</summary>
    public string Tag { get; set; } = string.Empty;
    
    /// <summary>Normalized (lowercase) for prefix lookup.</summary>
    public string NormalizedTag { get; set; } = string.Empty;
}