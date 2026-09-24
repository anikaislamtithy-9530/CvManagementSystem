using CvManagement.Domain.Entities;
using CvManagement.Domain.Enums;

namespace CvManagement.Application.Interfaces;

public interface IAttributeService
{
    Task<List<AttributeDefinition>> GetAllAsync(
        string? search = null,
        AttributeCategory? category = null);

    Task<AttributeDefinition?> GetByIdAsync(Guid id);

    Task<(bool Success, string? Error, AttributeDefinition? Attribute)>
        CreateAsync(AttributeDefinition attribute, string userId);

    Task<(bool Success, string? Error)>
        UpdateAsync(AttributeDefinition attribute, string userId);

    Task<(bool Success, string? Error)>
        DeleteAsync(Guid id, string userId);

    Task<bool> IsNameUniqueAsync(string name, Guid? excludeId = null);

    Task<(int PositionCount, int CvCount)> GetUsageStatsAsync(Guid id);
}