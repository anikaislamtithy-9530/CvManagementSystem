using CvManagement.Domain.Common;
using CvManagement.Domain.Enums;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Killer Feature #2: Position = customizable CV template.
/// All Recruiters share the same pool of positions.
/// </summary>
public class Position : BaseEntity
{
    // --- Basic info ---
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Location { get; set; }
    public PositionLevel Level { get; set; } = PositionLevel.Middle;
    
    // --- Access Rules ---
    /// <summary>
    /// If true, any authenticated user can apply.
    /// If false, AccessRulesJson is evaluated.
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    /// JSON-serialized list of AccessRule.
    /// E.g., [{"AttrId":"...","Op":"GreaterThan","Value":"7.0"}]
    /// </summary>
    public string? AccessRulesJson { get; set; }
    
    // --- Project settings ---
    
    /// <summary>
    /// Comma-separated tags for filtering which projects appear
    /// in generated CV. Empty = all projects.
    /// </summary>
    public string? ProjectTagsFilter { get; set; }
    
    /// <summary>Max number of projects to include in CV.</summary>
    public int MaxProjects { get; set; } = 5;
    
    // --- Audit ---
    public string CreatedByUserId { get; set; } = string.Empty;
    
    // --- Navigation ---
    public ICollection<PositionAttribute> Attributes { get; set; } = new List<PositionAttribute>();
    public ICollection<Cv> Cvs { get; set; } = new List<Cv>();
    public ICollection<DiscussionPost> Posts { get; set; } = new List<DiscussionPost>();
}