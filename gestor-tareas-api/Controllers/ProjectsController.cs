using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Models;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Controllers;

[ApiController, Authorize]
[Route("api/v1/proyectos")]
public sealed class ProjectsController(AppDbContext db, IProjectAccessService access, ILogger<ProjectsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var projects = await db.ProjectMembers.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Project.CreatedAt)
            .Select(x => new ProjectSummaryDto(x.ProjectId, x.Project.Name, x.Project.Description, x.Project.Color,
                x.Project.CreatedAt, x.Project.OwnerId, x.Role, x.Project.Tasks.Count,
                x.Project.Tasks.Count(t => t.Status == WorkItemStatus.Done)))
            .ToListAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await access.GetRoleAsync(id, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        return Ok(await BuildDetail(id, role.Value, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDetailDto>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var project = new Project
        {
            Name = request.Name.Trim(), Description = request.Description?.Trim(), Color = request.Color.ToUpperInvariant(), OwnerId = userId
        };
        project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId, Role = ProjectRole.Owner });
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Proyecto {ProjectId} creado por {UserId}", project.Id, userId);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, await BuildDetail(project.Id, ProjectRole.Owner, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> Update(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var role = await access.GetRoleAsync(id, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        var project = await db.Projects.SingleAsync(x => x.Id == id, cancellationToken);
        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.Color = request.Color.ToUpperInvariant();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await BuildDetail(id, role.Value, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = await access.GetRoleAsync(id, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role != ProjectRole.Owner) return Forbid();
        if (await db.Tasks.AnyAsync(x => x.ProjectId == id, cancellationToken))
            return Conflict(new ProblemDetails { Status = 409, Title = "El proyecto todavía tiene tareas", Detail = "Elimina o transfiere las tareas antes de borrar el proyecto." });
        var project = await db.Projects.SingleAsync(x => x.Id == id, cancellationToken);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/miembros")]
    public async Task<ActionResult<MemberDto>> Invite(Guid id, InviteMemberRequest request, CancellationToken cancellationToken)
    {
        var role = await access.GetRoleAsync(id, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role != ProjectRole.Owner) return Forbid();
        if (request.Role == ProjectRole.Owner)
            return BadRequest(new ProblemDetails { Status = 400, Title = "El propietario no se puede asignar mediante una invitación" });
        var email = request.Email.Trim().ToLowerInvariant();
        var invitedUser = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (invitedUser is null)
            return NotFound(new ProblemDetails { Status = 404, Title = "Usuario no encontrado", Detail = "La persona debe registrarse antes de ser invitada." });
        if (await db.ProjectMembers.AnyAsync(x => x.ProjectId == id && x.UserId == invitedUser.Id, cancellationToken))
            return Conflict(new ProblemDetails { Status = 409, Title = "El usuario ya pertenece al proyecto" });
        var member = new ProjectMember { ProjectId = id, UserId = invitedUser.Id, Role = request.Role };
        db.ProjectMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new MemberDto(invitedUser.Id, invitedUser.Name, invitedUser.Email, member.Role, member.JoinedAt, invitedUser.GetProfileImageUrl()));
    }

    [HttpPatch("{id:guid}/miembros/{userId:guid}")]
    public async Task<ActionResult<MemberDto>> ChangeRole(Guid id, Guid userId, ChangeMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await access.GetRoleAsync(id, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role != ProjectRole.Owner) return Forbid();
        if (request.Role == ProjectRole.Owner)
            return BadRequest(new ProblemDetails { Status = 400, Title = "No se puede transferir la propiedad desde esta acción" });
        var member = await db.ProjectMembers.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.ProjectId == id && x.UserId == userId, cancellationToken);
        if (member is null) return NotFound();
        if (member.Role == ProjectRole.Owner) return BadRequest(new ProblemDetails { Status = 400, Title = "No se puede modificar al propietario" });
        member.Role = request.Role;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new MemberDto(member.UserId, member.User.Name, member.User.Email, member.Role, member.JoinedAt, member.User.GetProfileImageUrl()));
    }

    [HttpDelete("{id:guid}/miembros/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var role = await access.GetRoleAsync(id, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role != ProjectRole.Owner) return Forbid();
        var member = await db.ProjectMembers.SingleOrDefaultAsync(x => x.ProjectId == id && x.UserId == userId, cancellationToken);
        if (member is null) return NotFound();
        if (member.Role == ProjectRole.Owner)
            return BadRequest(new ProblemDetails { Status = 400, Title = "No se puede remover al propietario" });
        var assignedTasks = await db.Tasks.Where(x => x.ProjectId == id && x.AssignedToId == userId).ToListAsync(cancellationToken);
        foreach (var task in assignedTasks) task.AssignedToId = null;
        db.ProjectMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ProjectDetailDto> BuildDetail(Guid id, ProjectRole role, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking().Include(x => x.Members).ThenInclude(x => x.User)
            .SingleAsync(x => x.Id == id, cancellationToken);
        return new ProjectDetailDto(project.Id, project.Name, project.Description, project.Color, project.CreatedAt,
            project.OwnerId, role, project.Members.OrderBy(member => member.Role)
                .Select(member => new MemberDto(member.UserId, member.User.Name, member.User.Email, member.Role,
                    member.JoinedAt, member.User.GetProfileImageUrl())).ToList());
    }
}
