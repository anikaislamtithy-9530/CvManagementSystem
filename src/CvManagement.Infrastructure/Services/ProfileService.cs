using CvManagement.Application.Interfaces;
using CvManagement.Domain.Entities;
using CvManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CvManagement.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(ApplicationDbContext db, ILogger<ProfileService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Profile?> GetByUserIdAsync(string userId)
    {
        return await _db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Profile?> GetWithAttributesAsync(string userId)
    {
        return await _db.Profiles
            .Include(p => p.Attributes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Profile?> GetFullProfileAsync(string userId)
    {
        return await _db.Profiles
            .Include(p => p.User)
            .Include(p => p.Attributes)
                .ThenInclude(a => a.AttributeDefinition)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<(bool Success, string? Error)> SaveAttributeAsync(
        string userId,
        Guid attributeDefinitionId,
        string? value,
        uint version)
    {
        try
        {
            var profile = await _db.Profiles
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                // Create profile if missing
                profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Profiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            // Find or create attribute
            var attr = profile.Attributes
                .FirstOrDefault(a => a.AttributeDefinitionId == attributeDefinitionId);

            if (attr == null)
            {
                attr = new ProfileAttribute
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profile.Id,
                    AttributeDefinitionId = attributeDefinitionId,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.ProfileAttributes.Add(attr);
            }
            else
            {
                // Optimistic locking check
                if (version > 0 && attr.Version != version)
                {
                    return (false, "This attribute was modified in another tab. Please refresh.");
                }

                attr.Value = value;
                attr.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Conflict detected. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving profile attribute");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> AddAttributeAsync(
        string userId,
        Guid attributeDefinitionId)
    {
        try
        {
            var profile = await _db.Profiles
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Profiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            var exists = profile.Attributes
                .Any(a => a.AttributeDefinitionId == attributeDefinitionId);

            if (exists)
                return (false, "Attribute already added.");

            _db.ProfileAttributes.Add(new ProfileAttribute
            {
                Id = Guid.NewGuid(),
                ProfileId = profile.Id,
                AttributeDefinitionId = attributeDefinitionId,
                Value = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attribute");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> RemoveAttributeAsync(
        string userId,
        Guid attributeDefinitionId)
    {
        try
        {
            var profile = await _db.Profiles
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return (false, "Profile not found.");

            var attr = profile.Attributes
                .FirstOrDefault(a => a.AttributeDefinitionId == attributeDefinitionId);

            if (attr == null)
                return (false, "Attribute not in profile.");

            _db.ProfileAttributes.Remove(attr);
            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing attribute");
            return (false, ex.Message);
        }
    }

    public async Task<Dictionary<Guid, string?>> GetAttributeValuesAsync(string userId)
    {
        var profile = await _db.Profiles
            .Include(p => p.Attributes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null) return new Dictionary<Guid, string?>();

        return profile.Attributes
            .ToDictionary(a => a.AttributeDefinitionId, a => a.Value);
    }
}