using CvManagement.Domain.Entities;
using CvManagement.Domain.Enums;

namespace CvManagement.Application.Interfaces;

public interface IPositionService
{
    Task<List<Position>> GetAllAsync(string? search = null, PositionLevel? level = null);

    Task<Position?> GetByIdAsync(Guid id);

    Task<Position?> GetByIdWithAttributesAsync(Guid id);

    Task<(bool Success, string? Error, Position? Position)>
        CreateAsync(Position position, List<Guid> attributeIds, string userId);

    Task<(bool Success, string? Error)>
        UpdateAsync(Position position, List<Guid> attributeIds, string userId);

    Task<(bool Success, string? Error)>
        DeleteAsync(Guid id, string userId);

    Task<(bool Success, string? Error, Position? Position)>
        DuplicateAsync(Guid id, string userId);

    Task<int> GetCvCountAsync(Guid positionId);

    Task<List<Position>> GetPopularAsync(int count = 5);

    Task<List<Position>> GetLatestAsync(int count = 5);
}