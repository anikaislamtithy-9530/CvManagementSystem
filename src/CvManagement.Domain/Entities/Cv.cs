using CvManagement.Domain.Common;
using CvManagement.Domain.Enums;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Killer Feature #3: Auto-generated CV.
/// One CV per (candidate, position) pair.
/// </summary>
public class Cv : BaseEntity
{
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    
    public string CandidateId { get; set; } = string.Empty;
    public ApplicationUser Candidate { get; set; } = null!;
    
    public CvStatus Status { get; set; } = CvStatus.Draft;
    
    /// <summary>Set when candidate publishes the CV.</summary>
    public DateTime? PublishedAt { get; set; }
    
    // --- Navigation ---
    public ICollection<CvAttribute> Attributes { get; set; } = new List<CvAttribute>();
    public ICollection<CvProject> Projects { get; set; } = new List<CvProject>();
    public ICollection<CvLike> Likes { get; set; } = new List<CvLike>();
}