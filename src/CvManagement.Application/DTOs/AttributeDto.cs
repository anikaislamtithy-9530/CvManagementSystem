using CvManagement.Domain.Enums;

namespace CvManagement.Application.DTOs;

public class AttributeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AttributeCategory Category { get; set; }
    public AttributeDataType DataType { get; set; }
    public string? DropdownOptionsJson { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? RegexPattern { get; set; }
    public string? Unit { get; set; }
    public bool IsBuiltIn { get; set; }
    public uint Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}