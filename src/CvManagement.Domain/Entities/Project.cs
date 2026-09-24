using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Candidate's project entry. Appears in profile and CVs
/// (if CV's position tag filter matches).
/// </summary>
public class Project : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    /// <summary>Markdown-formatted description.</summary>
    public string Description { get; set; } = string.Empty;
    
    // Navigation
    public ICollection<ProjectTag> Tags { get; set; } = new List<ProjectTag>();
    public ICollection<CvProject> CvProjects { get; set; } = new List<CvProject>();
}