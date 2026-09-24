using CvManagement.Application.Interfaces;
using CvManagement.Domain.Entities;
using CvManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CvManagement.Infrastructure.Services;

public class DiscussionService : IDiscussionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DiscussionService> _logger;

    public DiscussionService(ApplicationDbContext db, ILogger<DiscussionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<DiscussionPost>> GetPositionPostsAsync(Guid positionId)
    {
        return await _db.DiscussionPosts
            .Include(p => p.Author)
            .Where(p => p.PositionId == positionId)
            .OrderBy(p => p.CreatedAt)  // chronological order
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DiscussionPost?> GetByIdAsync(Guid id)
    {
        return await _db.DiscussionPosts
            .Include(p => p.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(bool Success, string? Error, DiscussionPost? Post)> CreateAsync(
        Guid positionId, 
        string authorId, 
        string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return (false, "Content cannot be empty.", null);

            // Verify position exists
            var positionExists = await _db.Positions.AnyAsync(p => p.Id == positionId);
            if (!positionExists)
                return (false, "Position not found.", null);

            // Verify author exists
            var authorExists = await _db.Users.AnyAsync(u => u.Id == authorId);
            if (!authorExists)
                return (false, "Author not found.", null);

            var post = new DiscussionPost
            {
                Id = Guid.NewGuid(),
                PositionId = positionId,
                AuthorId = authorId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.DiscussionPosts.Add(post);
            await _db.SaveChangesAsync();

            // Load author for return
            post.Author = await _db.Users.FindAsync(authorId) ?? null!;

            _logger.LogInformation("Discussion post created on position {PositionId} by {AuthorId}", 
                positionId, authorId);

            return (true, null, post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discussion post");
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        Guid postId, 
        string userId, 
        string content)
    {
        try
        {
            var post = await _db.DiscussionPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
                return (false, "Post not found.");

            if (post.AuthorId != userId)
                return (false, "You can only edit your own posts.");

            if (string.IsNullOrWhiteSpace(content))
                return (false, "Content cannot be empty.");

            post.Content = content.Trim();
            post.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating discussion post");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(
        Guid postId, 
        string userId, 
        bool isAdmin)
    {
        try
        {
            var post = await _db.DiscussionPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
                return (false, "Post not found.");

            // Author can delete own, Admin can delete any
            if (post.AuthorId != userId && !isAdmin)
                return (false, "You can only delete your own posts.");

            _db.DiscussionPosts.Remove(post);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Discussion post {PostId} deleted by {UserId}", postId, userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting discussion post");
            return (false, ex.Message);
        }
    }

    public async Task<int> GetPostCountAsync(Guid positionId)
    {
        return await _db.DiscussionPosts.CountAsync(p => p.PositionId == positionId);
    }
}