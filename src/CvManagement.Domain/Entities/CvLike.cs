using CvManagement.Domain.Common;

namespace CvManagement.Domain.Entities;

/// <summary>
/// Like on a CV. Only Recruiters can like. One like per
/// (Recruiter, CV) pair.
/// </summary>
public class CvLike : BaseEntity
{
    public Guid CvId { get; set; }
    public Cv Cv { get; set; } = null!;
    
    public string RecruiterId { get; set; } = string.Empty;
    public ApplicationUser Recruiter { get; set; } = null!;
}