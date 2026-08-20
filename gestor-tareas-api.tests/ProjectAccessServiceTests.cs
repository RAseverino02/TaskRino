using GestorTareas.Api.Data;
using GestorTareas.Api.Models;
using GestorTareas.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Tests;

public sealed class ProjectAccessServiceTests
{
    [Fact]
    public async Task Access_IsContextualToProjectMembershipRole()
    {
        await using var db = CreateDatabase();
        var owner = new User { Name = "Owner", Email = "owner@test.com", PasswordHash = "hash" };
        var viewer = new User { Name = "Viewer", Email = "viewer@test.com", PasswordHash = "hash" };
        var stranger = new User { Name = "Stranger", Email = "stranger@test.com", PasswordHash = "hash" };
        var project = new Project { Name = "Proyecto", OwnerId = owner.Id };
        db.AddRange(owner, viewer, stranger, project,
            new ProjectMember { ProjectId = project.Id, UserId = owner.Id, Role = ProjectRole.Owner },
            new ProjectMember { ProjectId = project.Id, UserId = viewer.Id, Role = ProjectRole.Viewer });
        await db.SaveChangesAsync();
        var service = new ProjectAccessService(db);

        Assert.Equal(ProjectRole.Owner, await service.GetRoleAsync(project.Id, owner.Id));
        Assert.False(await service.CanEditAsync(project.Id, viewer.Id));
        Assert.Null(await service.GetRoleAsync(project.Id, stranger.Id));
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }
}
