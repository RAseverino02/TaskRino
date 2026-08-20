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
public sealed class CommentsController(AppDbContext db, IProjectAccessService access) : ControllerBase
{
    [HttpPost("api/v1/tareas/{id:guid}/comentarios")]
    public async Task<ActionResult<CommentDto>> Add(Guid id, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null) return NotFound();
        var role = await access.GetRoleAsync(task.ProjectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        var userId = User.GetUserId();
        var comment = new Comment { TaskId = id, UserId = userId, Content = request.Content.Trim() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        var author = await db.Users.SingleAsync(x => x.Id == userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new CommentDto(comment.Id, userId, author.Name, comment.Content, comment.CreatedAt, author.GetProfileImageUrl()));
    }

    [HttpDelete("api/v1/comentarios/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var comment = await db.Comments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (comment is null) return NotFound();
        if (comment.UserId != User.GetUserId()) return Forbid();
        db.Comments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
