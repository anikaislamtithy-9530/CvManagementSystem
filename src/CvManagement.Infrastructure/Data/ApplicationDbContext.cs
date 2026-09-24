using CvManagement.Domain.Common;
using CvManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CvManagement.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // --- DbSets ---
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileAttribute> ProfileAttributes => Set<ProfileAttribute>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();
    
    public DbSet<Cv> Cvs => Set<Cv>();
    public DbSet<CvAttribute> CvAttributes => Set<CvAttribute>();
    public DbSet<CvProject> CvProjects => Set<CvProject>();
    public DbSet<CvLike> CvLikes => Set<CvLike>();
    
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ==========================================
        // Optimistic Locking (PostgreSQL xmin)
        // ==========================================
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Version))
                    .IsRowVersion();
            }
        }

        // ==========================================
        // Unique Constraints
        // ==========================================
        
        // Attribute names are globally unique
        builder.Entity<AttributeDefinition>()
            .HasIndex(a => a.Name)
            .IsUnique();

        // One profile per user
        builder.Entity<Profile>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        // One CV per position per candidate
        builder.Entity<Cv>()
            .HasIndex(c => new { c.PositionId, c.CandidateId })
            .IsUnique();

        // One like per recruiter per CV
        builder.Entity<CvLike>()
            .HasIndex(l => new { l.CvId, l.RecruiterId })
            .IsUnique();

        // ==========================================
        // Indexes for Performance
        // ==========================================
        
        builder.Entity<ProjectTag>()
            .HasIndex(t => t.NormalizedTag);

        builder.Entity<AttributeDefinition>()
            .HasIndex(a => a.Category);

        builder.Entity<Position>()
            .HasIndex(p => p.UpdatedAt);

        builder.Entity<Cv>()
            .HasIndex(c => c.Status);

        builder.Entity<DiscussionPost>()
            .HasIndex(p => p.PositionId);

        // ==========================================
        // Relationships
        // ==========================================
        
        // Profile <-> User (1:1)
        builder.Entity<Profile>()
            .HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<Profile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProfileAttribute -> AttributeDefinition
        builder.Entity<ProfileAttribute>()
            .HasOne(pa => pa.AttributeDefinition)
            .WithMany(ad => ad.ProfileAttributes)
            .HasForeignKey(pa => pa.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // PositionAttribute -> AttributeDefinition
        builder.Entity<PositionAttribute>()
            .HasOne(pa => pa.AttributeDefinition)
            .WithMany(ad => ad.PositionAttributes)
            .HasForeignKey(pa => pa.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // CvAttribute -> AttributeDefinition
        builder.Entity<CvAttribute>()
            .HasOne(ca => ca.AttributeDefinition)
            .WithMany(ad => ad.CvAttributes)
            .HasForeignKey(ca => ca.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> Projects
        builder.Entity<Project>()
            .HasOne(p => p.User)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> CVs
        builder.Entity<Cv>()
            .HasOne(c => c.Candidate)
            .WithMany(u => u.Cvs)
            .HasForeignKey(c => c.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        // CvLike -> Recruiter
        builder.Entity<CvLike>()
            .HasOne(l => l.Recruiter)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.RecruiterId)
            .OnDelete(DeleteBehavior.Cascade);

        // DiscussionPost -> Position
        builder.Entity<DiscussionPost>()
            .HasOne(p => p.Position)
            .WithMany(pos => pos.Posts)
            .HasForeignKey(p => p.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        // DiscussionPost -> Author
        builder.Entity<DiscussionPost>()
            .HasOne(p => p.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update UpdatedAt on modify
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}