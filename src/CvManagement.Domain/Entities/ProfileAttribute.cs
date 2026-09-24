using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Value of a library attribute in a specific user's profile.
/// This is the "master value" that CVs reference.
/// </summary>
public class ProfileAttribute : BaseEntity
{
    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
    
    public Guid AttributeDefinitionId { get; set; }
    public AttributeDefinition AttributeDefinition { get; set; } = null!;
    
    /// <summary>
    /// String-encoded value. Actual interpretation depends on
    /// AttributeDefinition.DataType.
    /// </summary>
    public string? Value { get; set; }
}