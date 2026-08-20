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
public sealed class TasksController(AppDbContext db, IProjectAccessService access, IAttachmentStorage storage) : ControllerBase
{
    [HttpGet("api/v1/proyectos/{projectId:guid}/tareas")]
    public async Task<ActionResult<PagedResponse<TaskDto>>> GetAll(
        Guid projectId, [FromQuery] WorkItemStatus? estado, [FromQuery] PriorityLevel? prioridad,
        [FromQuery] Guid? asignadoA, [FromQuery] int page = 1, [FromQuery] int pageSize = 30,
        [FromQuery] string orden = "estado",
        CancellationToken cancellationToken = default)
    {
        var role = await access.GetRoleAsync(projectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Tasks.AsNoTracking().Where(x => x.ProjectId == projectId);
        if (estado.HasValue) query = query.Where(x => x.Status == estado.Value);
        if (prioridad.HasValue) query = query.Where(x => x.Priority == prioridad.Value);
        if (asignadoA.HasValue) query = query.Where(x => x.AssignedToId == asignadoA.Value);
        var total = await query.CountAsync(cancellationToken);
        query = orden.ToLowerInvariant() switch
        {
            "titulo" => query.OrderBy(x => x.Title),
            "vencimiento" => query.OrderBy(x => x.DueDate == null).ThenBy(x => x.DueDate),
            "recientes" => query.OrderByDescending(x => x.CreatedAt),
            "prioridad" => query.OrderByDescending(x => x.Priority).ThenBy(x => x.DueDate),
            _ => query.OrderBy(x => x.Status).ThenByDescending(x => x.Priority).ThenBy(x => x.DueDate)
        };
        var tasks = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(x => x.AssignedTo).Include(x => x.Comments).ThenInclude(x => x.User).Include(x => x.Attachments)
            .AsSplitQuery().ToListAsync(cancellationToken);
        return Ok(new PagedResponse<TaskDto>(tasks.Select(ToDto).ToList(), page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize)));
    }

    [HttpGet("api/v1/tareas/{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await LoadTask(id, cancellationToken);
        if (task is null) return NotFound();
        if (await access.GetRoleAsync(task.ProjectId, User.GetUserId(), cancellationToken) is null) return NotFound();
        return Ok(ToDto(task));
    }

    [HttpPost("api/v1/proyectos/{projectId:guid}/tareas")]
    public async Task<ActionResult<TaskDto>> Create(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var role = await access.GetRoleAsync(projectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        if (!await IsValidAssignee(projectId, request.AssignedToId, cancellationToken)) return InvalidAssignee();
        var task = new WorkItem
        {
            ProjectId = projectId, Title = request.Title.Trim(), Description = request.Description?.Trim(),
            Status = request.Status, Priority = request.Priority, DueDate = request.DueDate,
            AssignedToId = request.AssignedToId
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        task = (await LoadTask(task.Id, cancellationToken))!;
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToDto(task));
    }

    [HttpPut("api/v1/tareas/{id:guid}")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null) return NotFound();
        var role = await access.GetRoleAsync(task.ProjectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        if (!await IsValidAssignee(task.ProjectId, request.AssignedToId, cancellationToken)) return InvalidAssignee();
        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.AssignedToId = request.AssignedToId;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto((await LoadTask(id, cancellationToken))!));
    }

    [HttpPatch("api/v1/tareas/{id:guid}/estado")]
    public async Task<ActionResult<TaskDto>> ChangeStatus(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null) return NotFound();
        var role = await access.GetRoleAsync(task.ProjectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        task.Status = request.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto((await LoadTask(id, cancellationToken))!));
    }

    [HttpDelete("api/v1/tareas/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.Include(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null) return NotFound();
        var role = await access.GetRoleAsync(task.ProjectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        foreach (var attachment in task.Attachments)
            await storage.DeleteAsync(attachment.RelativePath, cancellationToken);
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<WorkItem?> LoadTask(Guid id, CancellationToken cancellationToken) => db.Tasks.AsNoTracking()
        .Include(x => x.AssignedTo).Include(x => x.Comments).ThenInclude(x => x.User).Include(x => x.Attachments)
        .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    private async Task<bool> IsValidAssignee(Guid projectId, Guid? assignedToId, CancellationToken cancellationToken) =>
        assignedToId is null || await db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == assignedToId, cancellationToken);

    private BadRequestObjectResult InvalidAssignee() => BadRequest(new ProblemDetails
    { Status = 400, Title = "Asignación inválida", Detail = "La persona asignada debe pertenecer al proyecto." });

    private static TaskDto ToDto(WorkItem task) => new(task.Id, task.Title, task.Description, task.Status, task.Priority,
        task.DueDate, task.ProjectId, task.AssignedToId, task.AssignedTo?.Name, task.CreatedAt, task.UpdatedAt,
        task.Comments.OrderBy(x => x.CreatedAt).Select(x => new CommentDto(x.Id, x.UserId, x.User.Name, x.Content, x.CreatedAt, x.User.GetProfileImageUrl())).ToList(),
        task.Attachments.OrderByDescending(x => x.UploadedAt).Select(x => new AttachmentDto(x.Id, x.FileName, x.ContentType, x.SizeBytes, x.UploadedAt)).ToList(),
        task.AssignedTo?.GetProfileImageUrl());
}
