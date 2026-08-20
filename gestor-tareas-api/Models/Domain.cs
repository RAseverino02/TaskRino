using System.ComponentModel.DataAnnotations;

namespace GestorTareas.Api.Models;

public enum ProjectRole { Owner, Editor, Viewer }
public enum WorkItemStatus { ToDo, InProgress, Done }
public enum PriorityLevel { Baja, Media, Alta }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public required string Name { get; set; }
    [MaxLength(254)] public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(600)] public string? ProfileImagePath { get; set; }
    [MaxLength(120)] public string? ProfileImageContentType { get; set; }
    public ICollection<Project> OwnedProjects { get; set; } = [];
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(120)] public required string Name { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(7)] public string Color { get; set; } = "#1C6E72";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<WorkItem> Tasks { get; set; } = [];
}

public sealed class ProjectMember
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ProjectRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(160)] public required string Title { get; set; }
    [MaxLength(4000)] public string? Description { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.ToDo;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Media;
    public DateTimeOffset? DueDate { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
}

public sealed class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public WorkItem Task { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(2000)] public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public WorkItem Task { get; set; } = null!;
    [MaxLength(255)] public required string FileName { get; set; }
    [MaxLength(600)] public required string RelativePath { get; set; }
    [MaxLength(120)] public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(64)] public required string TokenHash { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    [MaxLength(64)] public string? ReplacedByTokenHash { get; set; }
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
