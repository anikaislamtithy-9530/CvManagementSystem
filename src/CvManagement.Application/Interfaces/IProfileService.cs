using CvManagement.Domain.Entities;

namespace CvManagement.Application.Interfaces;

public interface IProfileService
{
    Task<Profile?> GetByUserIdAsync(string userId);
    
    Task<Profile?> GetWithAttributesAsync(string userId);
    
    Task<Profile?> GetFullProfileAsync(string userId);
    
    Task<(bool Success, string? Error)> SaveAttributeAsync(
        string userId, 
        Guid attributeDefinitionId, 
        string? value,
        uint version);
    
    Task<(bool Success, string? Error)> AddAttributeAsync(
        string userId, 
        Guid attributeDefinitionId);
    
    Task<(bool Success, string? Error)> RemoveAttributeAsync(
        string userId, 
        Guid attributeDefinitionId);
    
    Task<Dictionary<Guid, string?>> GetAttributeValuesAsync(string userId);
}