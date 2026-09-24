using CvManagement.Domain.Entities;

namespace CvManagement.Application.Interfaces;

public interface IUserManagementService
{
    Task<List<UserDto>> GetAllAsync(string? search = null, string? roleFilter = null);
    
    Task<UserDto?> GetByIdAsync(string userId);
    
    Task<(bool Success, string? Error)> BlockUserAsync(string userId);
    
    Task<(bool Success, string? Error)> UnblockUserAsync(string userId);
    
    Task<(bool Success, string? Error)> AssignRoleAsync(string userId, string role);
    
    Task<(bool Success, string? Error)> RemoveRoleAsync(string userId, string role);
    
    Task<(bool Success, string? Error)> DeleteUserAsync(string userId);
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}