namespace CvManagement.Domain.Enums;

public enum CvStatus
{
    Draft = 1,
    Published = 2,
    Hidden = 3
}

public enum PositionLevel
{
    Intern = 0,
    Junior = 1,
    Middle = 2,
    Senior = 3,
    CLevel = 4
}

public enum AppRole
{
    Candidate = 1,
    Recruiter = 2,
    Administrator = 3
}

/// <summary>
/// Operators for access rules. Which operator is valid
/// depends on the attribute's data type.
/// </summary>
public enum RuleOperator
{
    Equals = 1,           // ==
    NotEquals = 2,        // !=
    GreaterThan = 3,      // >
    LessThan = 4,         // <
    GreaterOrEqual = 5,   // >=
    LessOrEqual = 6,      // <=
    Contains = 7,         // string contains
    In = 8                // dropdown multi-select
}