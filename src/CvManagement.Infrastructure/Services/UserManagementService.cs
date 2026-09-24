using CvManagement.Application.Interfaces;
using CvManagement.Domain.Entities;
using CvManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CvManagement.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync(string? search = null, string? roleFilter = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Take(500)
            .AsNoTracking()
            .ToListAsync();

        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            // Role filter
            if (!string.IsNullOrWhiteSpace(roleFilter) && !roles.Contains(roleFilter))
                continue;

            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Location = user.Location,
                IsBlocked = user.IsBlocked,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            });
        }

        return result;
    }

    public async Task<UserDto?> GetByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Location = user.Location,
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };
    }

    public async Task<(bool Success, string? Error)> BlockUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            user.IsBlocked = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} blocked", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking user");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> UnblockUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            user.IsBlocked = false;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} unblocked", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking user");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> AssignRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            var roleExists = await _db.Roles.AnyAsync(r => r.Name == role);
            if (!roleExists) return (false, $"Role '{role}' does not exist.");

            var alreadyHas = await _userManager.IsInRoleAsync(user, role);
            if (alreadyHas) return (false, $"User already has role '{role}'.");

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            _logger.LogInformation("Role {Role} assigned to user {UserId}", role, userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> RemoveRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            var hasRole = await _userManager.IsInRoleAsync(user, role);
            if (!hasRole) return (false, $"User does not have role '{role}'.");

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            _logger.LogInformation("Role {Role} removed from user {UserId}", role, userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            // Prevent deleting self
            // (handled at controller level if needed)

            // Delete related data (cascade should handle, but explicit for safety)
            var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null)
            {
                var profileAttributes = await _db.ProfileAttributes
                    .Where(pa => pa.ProfileId == profile.Id).ToListAsync();
                _db.ProfileAttributes.RemoveRange(profileAttributes);
                _db.Profiles.Remove(profile);
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return (false, ex.Message);
        }
    }
}