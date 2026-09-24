using CvManagement.Application.Interfaces;
using CvManagement.Domain.Entities;
using CvManagement.Domain.Enums;
using CvManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CvManagement.Infrastructure.Services;

public class CvService : ICvService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CvService> _logger;

    public CvService(ApplicationDbContext db, ILogger<CvService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ========================================
    // QUERIES
    // ========================================

    public async Task<List<Cv>> GetMyCvsAsync(string userId)
    {
        return await _db.Cvs
            .Include(c => c.Position)
            .Include(c => c.Likes)
            .Where(c => c.CandidateId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Cv>> GetAllCvsAsync(string? search = null, Guid? positionId = null)
    {
        var query = _db.Cvs
            .Include(c => c.Position)
            .Include(c => c.Candidate)
            .Include(c => c.Likes)
            .Where(c => c.Status == CvStatus.Published)
            .AsQueryable();

        if (positionId.HasValue)
        {
            query = query.Where(c => c.PositionId == positionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Position.Title.ToLower().Contains(term) ||
                c.Candidate.FirstName.ToLower().Contains(term) ||
                c.Candidate.LastName.ToLower().Contains(term) ||
                c.Candidate.Email!.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(c => c.PublishedAt)
            .Take(500)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Cv?> GetByIdAsync(Guid id)
    {
        return await _db.Cvs
            .Include(c => c.Position)
            .Include(c => c.Candidate)
            .Include(c => c.Likes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cv?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _db.Cvs
            .Include(c => c.Position)
                .ThenInclude(p => p.Attributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
            .Include(c => c.Candidate)
            .Include(c => c.Attributes)
                .ThenInclude(ca => ca.AttributeDefinition)
            .Include(c => c.Projects)
                .ThenInclude(cp => cp.Project)
                    .ThenInclude(p => p.Tags)
            .Include(c => c.Likes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cv?> GetByPositionAndCandidateAsync(Guid positionId, string candidateId)
    {
        return await _db.Cvs
            .Include(c => c.Position)
            .Include(c => c.Attributes)
            .Include(c => c.Projects)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => 
                c.PositionId == positionId && c.CandidateId == candidateId);
    }

    // ========================================
    // MUTATIONS
    // ========================================

    public async Task<(bool Success, string? Error, Cv? Cv)> CreateAsync(
        Guid positionId, 
        string candidateId)
    {
        try
        {
            // Check position exists
            var position = await _db.Positions
                .Include(p => p.Attributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                .FirstOrDefaultAsync(p => p.Id == positionId);

            if (position == null)
                return (false, "Position not found.", null);

            // Check candidate doesn't already have a CV for this position
            var existing = await _db.Cvs
                .AnyAsync(c => c.PositionId == positionId && c.CandidateId == candidateId);

            if (existing)
                return (false, "You already have a CV for this position.", null);

            // Check access
            var canAccess = await CanAccessPositionAsync(positionId, candidateId);
            if (!canAccess)
                return (false, "You don't have access to this position.", null);

            // Create CV
            var cv = new Cv
            {
                Id = Guid.NewGuid(),
                PositionId = positionId,
                CandidateId = candidateId,
                Status = CvStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Auto-fill attributes from profile
            var profileValues = await GetProfileValuesForAttributesAsync(
                candidateId, 
                position.Attributes.Select(pa => pa.AttributeDefinitionId).ToList());

            foreach (var pa in position.Attributes)
            {
                cv.Attributes.Add(new CvAttribute
                {
                    Id = Guid.NewGuid(),
                    CvId = cv.Id,
                    AttributeDefinitionId = pa.AttributeDefinitionId,
                    Value = profileValues.GetValueOrDefault(pa.AttributeDefinitionId),
                    IsInheritedFromProfile = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Auto-select projects based on tags filter
            var selectedProjects = await GetProjectsForPositionAsync(
                candidateId, position.ProjectTagsFilter, position.MaxProjects);

            for (int i = 0; i < selectedProjects.Count; i++)
            {
                cv.Projects.Add(new CvProject
                {
                    Id = Guid.NewGuid(),
                    CvId = cv.Id,
                    ProjectId = selectedProjects[i].Id,
                    DisplayOrder = i,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _db.Cvs.Add(cv);
            await _db.SaveChangesAsync();

            _logger.LogInformation("CV created for candidate {CandidateId} on position {PositionId}", 
                candidateId, positionId);

            return (true, null, cv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating CV");
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string? Error)> SaveAttributeAsync(
        Guid cvId, 
        Guid attributeDefinitionId, 
        string? value)
    {
        try
        {
            var cv = await _db.Cvs
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.Id == cvId);

            if (cv == null)
                return (false, "CV not found.");

            var cvAttr = cv.Attributes
                .FirstOrDefault(a => a.AttributeDefinitionId == attributeDefinitionId);

            if (cvAttr == null)
                return (false, "Attribute not part of this CV.");

            cvAttr.Value = value;
            cvAttr.IsInheritedFromProfile = false;
            cvAttr.UpdatedAt = DateTime.UtcNow;

            // **IMPORTANT:** Update master value in profile
            var profile = await _db.Profiles
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.UserId == cv.CandidateId);

            if (profile != null)
            {
                var profileAttr = profile.Attributes
                    .FirstOrDefault(a => a.AttributeDefinitionId == attributeDefinitionId);

                if (profileAttr != null)
                {
                    profileAttr.Value = value;
                    profileAttr.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Add to profile if missing
                    _db.ProfileAttributes.Add(new ProfileAttribute
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = profile.Id,
                        AttributeDefinitionId = attributeDefinitionId,
                        Value = value,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving CV attribute");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> PublishAsync(Guid cvId, string userId)
    {
        try
        {
            var cv = await _db.Cvs
                .Include(c => c.Attributes)
                    .ThenInclude(ca => ca.AttributeDefinition)
                .FirstOrDefaultAsync(c => c.Id == cvId);

            if (cv == null)
                return (false, "CV not found.");

            if (cv.CandidateId != userId)
                return (false, "You can only publish your own CV.");

            // Check all required attributes are filled
            var emptyRequired = cv.Attributes
                .Where(a => a.AttributeDefinition.DataType != AttributeDataType.Boolean)
                .Where(a => string.IsNullOrWhiteSpace(a.Value))
                .ToList();

            if (emptyRequired.Any())
            {
                var names = string.Join(", ", emptyRequired.Select(a => a.AttributeDefinition.Name));
                return (false, $"Please fill all attributes before publishing: {names}");
            }

            cv.Status = CvStatus.Published;
            cv.PublishedAt = DateTime.UtcNow;
            cv.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            _logger.LogInformation("CV {CvId} published", cvId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing CV");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> UnpublishAsync(Guid cvId, string userId)
    {
        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId);
        if (cv == null) return (false, "CV not found.");
        if (cv.CandidateId != userId) return (false, "Not your CV.");

        cv.Status = CvStatus.Draft;
        cv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid cvId, string userId)
    {
        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId);
        if (cv == null) return (false, "CV not found.");
        if (cv.CandidateId != userId) return (false, "Not your CV.");

        _db.Cvs.Remove(cv);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ========================================
    // ACCESS CONTROL
    // ========================================

    public async Task<bool> CanAccessPositionAsync(Guid positionId, string candidateId)
    {
        var position = await _db.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position == null) return false;

        // Public positions accessible to all authenticated users
        if (position.IsPublic) return true;

        // Restricted: check access rules
        if (string.IsNullOrEmpty(position.AccessRulesJson)) return true; // no rules = accessible

        try
        {
            var rules = JsonSerializer.Deserialize<List<AccessRule>>(position.AccessRulesJson);
            if (rules == null || !rules.Any()) return true;

            // Get candidate's profile attributes
            var profile = await _db.Profiles
                .Include(p => p.Attributes)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == candidateId);

            if (profile == null) return false;

            // Evaluate each rule
            foreach (var rule in rules)
            {
                var attr = profile.Attributes
                    .FirstOrDefault(a => a.AttributeDefinitionId == rule.AttributeDefinitionId);

                if (attr == null || string.IsNullOrEmpty(attr.Value))
                    return false;

                if (!EvaluateRule(attr.Value, rule.Operator, rule.Value))
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating access rules");
            return false;
        }
    }

    private bool EvaluateRule(string actualValue, RuleOperator op, string expectedValue)
    {
        // Try numeric comparison first
        if (decimal.TryParse(actualValue, out var actualNum) && 
            decimal.TryParse(expectedValue, out var expectedNum))
        {
            return op switch
            {
                RuleOperator.Equals => actualNum == expectedNum,
                RuleOperator.NotEquals => actualNum != expectedNum,
                RuleOperator.GreaterThan => actualNum > expectedNum,
                RuleOperator.LessThan => actualNum < expectedNum,
                RuleOperator.GreaterOrEqual => actualNum >= expectedNum,
                RuleOperator.LessOrEqual => actualNum <= expectedNum,
                _ => false
            };
        }

        // Try boolean
        if (bool.TryParse(actualValue, out var actualBool) && 
            bool.TryParse(expectedValue, out var expectedBool))
        {
            return op switch
            {
                RuleOperator.Equals => actualBool == expectedBool,
                RuleOperator.NotEquals => actualBool != expectedBool,
                _ => false
            };
        }

        // String comparison
        return op switch
        {
            RuleOperator.Equals => actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase),
            RuleOperator.NotEquals => !actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase),
            RuleOperator.Contains => actualValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    // ========================================
    // LIKES
    // ========================================

    public async Task<(bool Success, string? Error)> ToggleLikeAsync(Guid cvId, string recruiterId)
    {
        try
        {
            var existing = await _db.CvLikes
                .FirstOrDefaultAsync(l => l.CvId == cvId && l.RecruiterId == recruiterId);

            if (existing != null)
            {
                _db.CvLikes.Remove(existing);
            }
            else
            {
                _db.CvLikes.Add(new CvLike
                {
                    Id = Guid.NewGuid(),
                    CvId = cvId,
                    RecruiterId = recruiterId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling like");
            return (false, ex.Message);
        }
    }

    public async Task<int> GetLikeCountAsync(Guid cvId)
    {
        return await _db.CvLikes.CountAsync(l => l.CvId == cvId);
    }

    public async Task<bool> HasLikedAsync(Guid cvId, string recruiterId)
    {
        return await _db.CvLikes
            .AnyAsync(l => l.CvId == cvId && l.RecruiterId == recruiterId);
    }

    // ========================================
    // AUTO-FILL HELPERS
    // ========================================

    public async Task<Dictionary<Guid, string?>> GetProfileValuesForAttributesAsync(
        string candidateId, 
        List<Guid> attributeIds)
    {
        var profile = await _db.Profiles
            .Include(p => p.Attributes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == candidateId);

        var result = new Dictionary<Guid, string?>();

        if (profile == null)
        {
            foreach (var id in attributeIds) result[id] = null;
            return result;
        }

        foreach (var attrId in attributeIds)
        {
            var profileAttr = profile.Attributes
                .FirstOrDefault(a => a.AttributeDefinitionId == attrId);
            result[attrId] = profileAttr?.Value;
        }

        return result;
    }

    private async Task<List<Project>> GetProjectsForPositionAsync(
        string candidateId, 
        string? tagFilter, 
        int maxProjects)
    {
        var query = _db.Projects
            .Include(p => p.Tags)
            .Where(p => p.UserId == candidateId)
            .OrderByDescending(p => p.StartDate)
            .AsQueryable();

        var projects = await query.Take(50).ToListAsync();

        // If tag filter specified, filter projects
        if (!string.IsNullOrWhiteSpace(tagFilter))
        {
            var tags = tagFilter
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower())
                .ToList();

            projects = projects
                .Where(p => p.Tags.Any(t => tags.Contains(t.NormalizedTag)))
                .ToList();
        }

        return projects.Take(maxProjects).ToList();
    }
}