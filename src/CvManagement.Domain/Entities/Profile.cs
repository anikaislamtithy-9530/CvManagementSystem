using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Personal profile. One per authenticated user.
/// Contains built-in "Me" attributes + user-selected library attributes.
/// </summary>
public class Profile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    // Navigation
    public ICollection<ProfileAttribute> Attributes { get; set; } = new List<ProfileAttribute>();
}