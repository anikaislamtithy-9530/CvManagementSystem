using CvManagement.Domain.Enums;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Value object for position access rules. NOT an entity.
/// Serialized to Position.AccessRulesJson.
/// </summary>
public class AccessRule
{
    public Guid AttributeDefinitionId { get; set; }
    public RuleOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;
}