using CvManagement.Domain.Entities;
using CvManagement.Domain.Enums;

namespace CvManagement.Application.Interfaces;

public interface ICvService
{
    // ========================================
    // QUERIES
    // ========================================
    Task<List<Cv>> GetMyCvsAsync(string userId);
    
    Task<List<Cv>> GetAllCvsAsync(string? search = null, Guid? positionId = null);
    
    Task<Cv?> GetByIdAsync(Guid id);
    
    Task<Cv?> GetByIdWithDetailsAsync(Guid id);
    
    Task<Cv?> GetByPositionAndCandidateAsync(Guid positionId, string candidateId);
    
    // ========================================
    // MUTATIONS
    // ========================================
    Task<(bool Success, string? Error, Cv? Cv)> CreateAsync(
        Guid positionId, 
        string candidateId);
    
    Task<(bool Success, string? Error)> SaveAttributeAsync(
        Guid cvId, 
        Guid attributeDefinitionId, 
        string? value);
    
    Task<(bool Success, string? Error)> PublishAsync(Guid cvId, string userId);
    
    Task<(bool Success, string? Error)> UnpublishAsync(Guid cvId, string userId);
    
    Task<(bool Success, string? Error)> DeleteAsync(Guid cvId, string userId);
    
    // ========================================
    // ACCESS CONTROL
    // ========================================
    Task<bool> CanAccessPositionAsync(Guid positionId, string candidateId);
    
    // ========================================
    // LIKES
    // ========================================
    Task<(bool Success, string? Error)> ToggleLikeAsync(Guid cvId, string recruiterId);
    
    Task<int> GetLikeCountAsync(Guid cvId);
    
    Task<bool> HasLikedAsync(Guid cvId, string recruiterId);
    
    // ========================================
    // AUTO-FILL
    // ========================================
    Task<Dictionary<Guid, string?>> GetProfileValuesForAttributesAsync(
        string candidateId, 
        List<Guid> attributeIds);
}