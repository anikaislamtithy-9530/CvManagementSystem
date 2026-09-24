using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Links a library attribute into a position's template.
/// Controls ordering and required-ness.
/// </summary>
public class PositionAttribute : BaseEntity
{
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    
    public Guid AttributeDefinitionId { get; set; }
    public AttributeDefinition AttributeDefinition { get; set; } = null!;
    
    /// <summary>Display order in the CV template.</summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>If true, CV cannot be published until filled.</summary>
    public bool IsRequired { get; set; } = false;
}