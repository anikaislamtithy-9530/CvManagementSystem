using Microsoft.AspNetCore.Identity;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with
/// custom profile fields (Me section - undeletable attributes).
/// </summary>
public class ApplicationUser : IdentityUser
{
    // --- "Me" section (built-in, undeletable attributes) ---
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PhotoUrl { get; set; }
    
    // --- Admin controls ---
    public bool IsBlocked { get; set; } = false;
    
    // --- Audit ---
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // --- Navigation ---
    public Profile? Profile { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Cv> Cvs { get; set; } = new List<Cv>();
    public ICollection<CvLike> Likes { get; set; } = new List<CvLike>();
    public ICollection<DiscussionPost> Posts { get; set; } = new List<DiscussionPost>();
}