namespace CvManagement.Domain.Enums;

/// <summary>
/// Supported attribute types. Each type has its own UI component
/// and validation rules.
/// </summary>
public enum AttributeDataType
{
    /// <summary>Single-line plain text</summary>
    String = 1,
    
    /// <summary>Multi-line Markdown-formatted text</summary>
    Text = 2,
    
    /// <summary>External image URL (uploaded to cloud storage)</summary>
    Image = 3,
    
    /// <summary>Numeric value (int or decimal)</summary>
    Numeric = 4,
    
    /// <summary>Single date</summary>
    Date = 5,
    
    /// <summary>Date range (start - end)</summary>
    Period = 6,
    
    /// <summary>Boolean checkbox</summary>
    Boolean = 7,
    
    /// <summary>Dropdown (One of many)</summary>
    OneOfMany = 8
}