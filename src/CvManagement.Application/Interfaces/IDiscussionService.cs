using CvManagement.Domain.Entities;

namespace CvManagement.Application.Interfaces;

public interface IDiscussionService
{
    Task<List<DiscussionPost>> GetPositionPostsAsync(Guid positionId);
    
    Task<DiscussionPost?> GetByIdAsync(Guid id);
    
    Task<(bool Success, string? Error, DiscussionPost? Post)> CreateAsync(
        Guid positionId, 
        string authorId, 
        string content);
    
    Task<(bool Success, string? Error)> UpdateAsync(
        Guid postId, 
        string userId, 
        string content);
    
    Task<(bool Success, string? Error)> DeleteAsync(
        Guid postId, 
        string userId, 
        bool isAdmin);
    
    Task<int> GetPostCountAsync(Guid positionId);
}