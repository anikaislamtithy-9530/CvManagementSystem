using CvManagement.Domain.Common;
using CvManagement.Domain.Enums;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Killer Feature #1: Attribute Library entry.
/// Attributes are defined once and reused across profiles,
/// positions, and CVs.
/// </summary>
public class AttributeDefinition : BaseEntity
{
    /// <summary>Globally unique name (e.g., "IELTS Score").</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Recruiter-facing description.</summary>
    public string Description { get; set; } = string.Empty;
    
    public AttributeCategory Category { get; set; }
    
    public AttributeDataType DataType { get; set; }
    
    // --- Type-specific config ---
    
    /// <summary>
    /// For OneOfMany type: JSON array of options.
    /// E.g., ["Beginner","Intermediate","Advanced"]
    /// </summary>
    public string? DropdownOptionsJson { get; set; }
    
    // --- Validation rules (optional) ---
    
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    
    /// <summary>Regex pattern for string validation.</summary>
    public string? RegexPattern { get; set; }
    
    /// <summary>Unit for numeric (e.g., "years", "score").</summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Built-in "Me" section attributes (First Name, Last Name, etc.).
    /// Cannot be deleted by Recruiters.
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;
    
    // --- Navigation ---
    public ICollection<ProfileAttribute> ProfileAttributes { get; set; } = new List<ProfileAttribute>();
    public ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public ICollection<CvAttribute> CvAttributes { get; set; } = new List<CvAttribute>();
}