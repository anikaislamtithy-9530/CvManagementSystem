using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Discussion post on a Position. Chronological order,
/// new posts appended at end.
/// </summary>
public class DiscussionPost : BaseEntity
{
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;
    
    /// <summary>Markdown-formatted content.</summary>
    public string Content { get; set; } = string.Empty;
}