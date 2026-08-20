using System.ComponentModel.DataAnnotations;
using GestorTareas.Api.Models;

namespace GestorTareas.Api.DTOs;

public sealed record UserDto(Guid Id, string Name, string Email, DateTimeOffset RegisteredAt, string? ProfileImageUrl);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, UserDto User);

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(100)] public string Password { get; set; } = string.Empty;
}
public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
public sealed class RefreshRequest { [Required] public string RefreshToken { get; set; } = string.Empty; }
public sealed class UpdateProfileRequest { [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty; }

public sealed record ProjectSummaryDto(Guid Id, string Name, string? Description, string Color, DateTimeOffset CreatedAt, Guid OwnerId, ProjectRole Role, int TaskCount, int CompletedTaskCount);
public sealed record ProjectDetailDto(Guid Id, string Name, string? Description, string Color, DateTimeOffset CreatedAt, Guid OwnerId, ProjectRole Role, IReadOnlyList<MemberDto> Members);
public sealed record MemberDto(Guid UserId, string Name, string Email, ProjectRole Role, DateTimeOffset JoinedAt, string? ProfileImageUrl);
public class CreateProjectRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")] public string Color { get; set; } = "#1C6E72";
}
public sealed class UpdateProjectRequest : CreateProjectRequest;
public sealed class InviteMemberRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [EnumDataType(typeof(ProjectRole))] public ProjectRole Role { get; set; } = ProjectRole.Editor;
}
public sealed class ChangeMemberRoleRequest { [EnumDataType(typeof(ProjectRole))] public ProjectRole Role { get; set; } }

public sealed record TaskDto(Guid Id, string Title, string? Description, WorkItemStatus Status, PriorityLevel Priority,
    DateTimeOffset? DueDate, Guid ProjectId, Guid? AssignedToId, string? AssignedToName, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, IReadOnlyList<CommentDto> Comments, IReadOnlyList<AttachmentDto> Attachments, string? AssignedToProfileImageUrl);
public class CreateTaskRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Title { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    [EnumDataType(typeof(WorkItemStatus))] public WorkItemStatus Status { get; set; }
    [EnumDataType(typeof(PriorityLevel))] public PriorityLevel Priority { get; set; } = PriorityLevel.Media;
    public DateTimeOffset? DueDate { get; set; }
    public Guid? AssignedToId { get; set; }
}
public sealed class UpdateTaskRequest : CreateTaskRequest;
public sealed class ChangeTaskStatusRequest { [EnumDataType(typeof(WorkItemStatus))] public WorkItemStatus Status { get; set; } }
public sealed record CommentDto(Guid Id, Guid UserId, string UserName, string Content, DateTimeOffset CreatedAt, string? UserProfileImageUrl);
public sealed class AddCommentRequest { [Required, StringLength(2000, MinimumLength = 1)] public string Content { get; set; } = string.Empty; }
public sealed record AttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset UploadedAt);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
