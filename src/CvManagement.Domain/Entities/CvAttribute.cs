using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Attribute value for a specific CV. When candidate edits a value
/// here, the change is synced back to ProfileAttribute (single
/// master value).
/// </summary>
public class CvAttribute : BaseEntity
{
    public Guid CvId { get; set; }
    public Cv Cv { get; set; } = null!;
    
    public Guid AttributeDefinitionId { get; set; }
    public AttributeDefinition AttributeDefinition { get; set; } = null!;
    
    public string? Value { get; set; }
    
    /// <summary>
    /// True if the value came from profile (not overridden).
    /// False if candidate edited it specifically for this CV.
    /// </summary>
    public bool IsInheritedFromProfile { get; set; } = true;
}