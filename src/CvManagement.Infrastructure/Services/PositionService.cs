using CvManagement.Application.Interfaces;
using CvManagement.Domain.Entities;
using CvManagement.Domain.Enums;
using CvManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CvManagement.Infrastructure.Services;

public class PositionService : IPositionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PositionService> _logger;

    public PositionService(ApplicationDbContext db, ILogger<PositionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Position>> GetAllAsync(string? search = null, PositionLevel? level = null)
    {
        var query = _db.Positions.Include(p => p.Attributes).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(term) ||
                p.ShortDescription.ToLower().Contains(term) ||
                (p.Company != null && p.Company.ToLower().Contains(term)));
        }

        if (level.HasValue)
            query = query.Where(p => p.Level == level.Value);

        return await query.OrderByDescending(p => p.UpdatedAt).Take(500).ToListAsync();
    }

    public async Task<Position?> GetByIdAsync(Guid id)
    {
        return await _db.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Position?> GetByIdWithAttributesAsync(Guid id)
    {
        return await _db.Positions
            .Include(p => p.Attributes)
                .ThenInclude(pa => pa.AttributeDefinition)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(bool Success, string? Error, Position? Position)>
        CreateAsync(Position position, List<Guid> attributeIds, string userId)
    {
        try
        {
            position.Id = Guid.NewGuid();
            position.CreatedAt = DateTime.UtcNow;
            position.UpdatedAt = DateTime.UtcNow;
            position.CreatedByUserId = userId;

            for (int i = 0; i < attributeIds.Count; i++)
            {
                position.Attributes.Add(new PositionAttribute
                {
                    Id = Guid.NewGuid(),
                    PositionId = position.Id,
                    AttributeDefinitionId = attributeIds[i],
                    DisplayOrder = i,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _db.Positions.Add(position);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Position {Title} created by {UserId}", position.Title, userId);
            return (true, null, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating position");
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error)>
        UpdateAsync(Position position, List<Guid> attributeIds, string userId)
    {
        try
        {
            var existing = await _db.Positions
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == position.Id);

            if (existing == null)
                return (false, "Position not found.");

            if (existing.Version != position.Version)
                return (false, "Position was modified by another user.");

            existing.Title = position.Title;
            existing.ShortDescription = position.ShortDescription;
            existing.Company = position.Company;
            existing.Location = position.Location;
            existing.Level = position.Level;
            existing.IsPublic = position.IsPublic;
            existing.AccessRulesJson = position.AccessRulesJson;
            existing.ProjectTagsFilter = position.ProjectTagsFilter;
            existing.MaxProjects = position.MaxProjects;
            existing.UpdatedAt = DateTime.UtcNow;

            _db.PositionAttributes.RemoveRange(existing.Attributes);

            for (int i = 0; i < attributeIds.Count; i++)
            {
                _db.PositionAttributes.Add(new PositionAttribute
                {
                    Id = Guid.NewGuid(),
                    PositionId = existing.Id,
                    AttributeDefinitionId = attributeIds[i],
                    DisplayOrder = i,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Position {Title} updated by {UserId}", position.Title, userId);
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Position was modified by another user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, string userId)
    {
        var position = await _db.Positions
            .Include(p => p.Cvs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
            return (false, "Position not found.");

        if (position.Cvs.Any())
            return (false, "Cannot delete position with existing CVs.");

        _db.Positions.Remove(position);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Position {Title} deleted by {UserId}", position.Title, userId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error, Position? Position)>
        DuplicateAsync(Guid id, string userId)
    {
        var source = await _db.Positions
            .Include(p => p.Attributes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (source == null)
            return (false, "Position not found.", null);

        var copy = new Position
        {
            Id = Guid.NewGuid(),
            Title = source.Title + " (Copy)",
            ShortDescription = source.ShortDescription,
            Company = source.Company,
            Location = source.Location,
            Level = source.Level,
            IsPublic = source.IsPublic,
            AccessRulesJson = source.AccessRulesJson,
            ProjectTagsFilter = source.ProjectTagsFilter,
            MaxProjects = source.MaxProjects,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var attr in source.Attributes)
        {
            copy.Attributes.Add(new PositionAttribute
            {
                Id = Guid.NewGuid(),
                PositionId = copy.Id,
                AttributeDefinitionId = attr.AttributeDefinitionId,
                DisplayOrder = attr.DisplayOrder,
                IsRequired = attr.IsRequired,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _db.Positions.Add(copy);
        await _db.SaveChangesAsync();

        return (true, null, copy);
    }

    public async Task<int> GetCvCountAsync(Guid positionId)
    {
        return await _db.Cvs.CountAsync(c => c.PositionId == positionId);
    }

    public async Task<List<Position>> GetPopularAsync(int count = 5)
    {
        return await _db.Positions
            .Include(p => p.Cvs)
            .OrderByDescending(p => p.Cvs.Count(c => c.Status == CvStatus.Published))
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Position>> GetLatestAsync(int count = 5)
    {
        return await _db.Positions
            .OrderByDescending(p => p.UpdatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }
}