using CvManagement.Application.Interfaces;
using CvManagement.Domain.Entities;
using CvManagement.Domain.Enums;
using CvManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CvManagement.Infrastructure.Services;

public class AttributeService : IAttributeService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AttributeService> _logger;

    public AttributeService(ApplicationDbContext db, ILogger<AttributeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<AttributeDefinition>> GetAllAsync(
        string? search = null, AttributeCategory? category = null)
    {
        var query = _db.AttributeDefinitions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(term) ||
                a.Description.ToLower().Contains(term));
        }

        if (category.HasValue)
        {
            query = query.Where(a => a.Category == category.Value);
        }

        return await query
            .OrderBy(a => a.IsBuiltIn ? 0 : 1)
            .ThenBy(a => a.Name)
            .Take(500)
            .ToListAsync();
    }

    public async Task<AttributeDefinition?> GetByIdAsync(Guid id)
    {
        return await _db.AttributeDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<(bool Success, string? Error, AttributeDefinition? Attribute)>
        CreateAsync(AttributeDefinition attribute, string userId)
    {
        // Validate uniqueness
        var existing = await _db.AttributeDefinitions
            .AnyAsync(a => a.Name == attribute.Name);
        if (existing)
            return (false, $"Attribute '{attribute.Name}' already exists.", null);

        attribute.Id = Guid.NewGuid();
        attribute.CreatedAt = DateTime.UtcNow;
        attribute.UpdatedAt = DateTime.UtcNow;

        _db.AttributeDefinitions.Add(attribute);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Attribute {Name} created by user {UserId}", 
            attribute.Name, userId);

        return (true, null, attribute);
    }

    public async Task<(bool Success, string? Error)>
        UpdateAsync(AttributeDefinition attribute, string userId)
    {
        var existing = await _db.AttributeDefinitions
            .FirstOrDefaultAsync(a => a.Id == attribute.Id);

        if (existing == null)
            return (false, "Attribute not found.");

        // Check name uniqueness (if changed)
        if (existing.Name != attribute.Name)
        {
            var nameTaken = await _db.AttributeDefinitions
                .AnyAsync(a => a.Name == attribute.Name && a.Id != attribute.Id);
            if (nameTaken)
                return (false, $"Attribute '{attribute.Name}' already exists.");
        }

        // Can't modify built-in type or name
        if (existing.IsBuiltIn)
        {
            if (existing.Name != attribute.Name)
                return (false, "Built-in attribute name cannot be changed.");
            if (existing.DataType != attribute.DataType)
                return (false, "Built-in attribute type cannot be changed.");
        }

        // Optimistic locking
        if (existing.Version != attribute.Version)
        {
            return (false, 
                "This attribute was modified by another user. Please refresh and try again.");
        }

        existing.Name = attribute.Name;
        existing.Description = attribute.Description;
        existing.Category = attribute.Category;
        existing.DataType = attribute.DataType;
        existing.DropdownOptionsJson = attribute.DropdownOptionsJson;
        existing.MinLength = attribute.MinLength;
        existing.MaxLength = attribute.MaxLength;
        existing.MinValue = attribute.MinValue;
        existing.MaxValue = attribute.MaxValue;
        existing.RegexPattern = attribute.RegexPattern;
        existing.Unit = attribute.Unit;
        existing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("Attribute {Name} updated by user {UserId}", 
                existing.Name, userId);
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, 
                "This attribute was modified by another user. Please refresh and try again.");
        }
    }

    public async Task<(bool Success, string? Error)>
        DeleteAsync(Guid id, string userId)
    {
        var attribute = await _db.AttributeDefinitions
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attribute == null)
            return (false, "Attribute not found.");

        if (attribute.IsBuiltIn)
            return (false, "Built-in attributes cannot be deleted.");

        // Check if used in any position or CV
        var usedInPositions = await _db.PositionAttributes
            .AnyAsync(pa => pa.AttributeDefinitionId == id);
        var usedInCvs = await _db.CvAttributes
            .AnyAsync(ca => ca.AttributeDefinitionId == id);
        var usedInProfiles = await _db.ProfileAttributes
            .AnyAsync(p => p.AttributeDefinitionId == id);

        if (usedInPositions || usedInCvs || usedInProfiles)
        {
            return (false, 
                "Attribute is in use. Remove it from positions, CVs, and profiles first.");
        }

        _db.AttributeDefinitions.Remove(attribute);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Attribute {Name} deleted by user {UserId}", 
            attribute.Name, userId);

        return (true, null);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludeId = null)
    {
        return !await _db.AttributeDefinitions
            .AnyAsync(a => a.Name == name && (!excludeId.HasValue || a.Id != excludeId));
    }

    public async Task<(int PositionCount, int CvCount)> GetUsageStatsAsync(Guid id)
    {
        var positionCount = await _db.PositionAttributes
            .CountAsync(pa => pa.AttributeDefinitionId == id);
        var cvCount = await _db.CvAttributes
            .CountAsync(ca => ca.AttributeDefinitionId == id);

        return (positionCount, cvCount);
    }
}