using GestorTareas.Api.Data;
using GestorTareas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Services;

public interface IProjectAccessService
{
    Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanEditAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed class ProjectAccessService(AppDbContext db) : IProjectAccessService
{
    public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default) =>
        db.ProjectMembers.Where(x => x.ProjectId == projectId && x.UserId == userId)
            .Select(x => (ProjectRole?)x.Role).SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> CanEditAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var role = await GetRoleAsync(projectId, userId, cancellationToken);
        return role is ProjectRole.Owner or ProjectRole.Editor;
    }
}
