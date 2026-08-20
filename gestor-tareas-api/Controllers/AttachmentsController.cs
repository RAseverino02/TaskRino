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
public sealed class AttachmentsController(AppDbContext db, IProjectAccessService access, IAttachmentStorage storage) : ControllerBase
{
    private const long MaxSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf" };

    [HttpPost("api/v1/tareas/{id:guid}/adjuntos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxSize + 1024 * 100)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null) return NotFound();
        var role = await access.GetRoleAsync(task.ProjectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        if (file.Length is 0 or > MaxSize)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Tamaño de archivo inválido", Detail = "El archivo debe pesar entre 1 byte y 5 MB." });
        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Tipo de archivo no permitido", Detail = "Solo se aceptan imágenes (JPG, PNG, GIF o WebP) y documentos PDF." });
        if (!await HasValidSignature(file, cancellationToken))
            return BadRequest(new ProblemDetails { Status = 400, Title = "El contenido del archivo no coincide con su tipo", Detail = "Elige una imagen o PDF válido." });

        var relativePath = await storage.SaveAsync(id, file, cancellationToken);
        var attachment = new Attachment
        {
            TaskId = id, FileName = Path.GetFileName(file.FileName), RelativePath = relativePath,
            ContentType = file.ContentType, SizeBytes = file.Length
        };
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            new AttachmentDto(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.UploadedAt));
    }

    [HttpGet("api/v1/adjuntos/{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.AsNoTracking().Include(x => x.Task)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (attachment is null) return NotFound();
        if (await access.GetRoleAsync(attachment.Task.ProjectId, User.GetUserId(), cancellationToken) is null) return NotFound();
        var stream = await storage.OpenReadAsync(attachment.RelativePath, cancellationToken);
        if (stream is null) return NotFound(new ProblemDetails { Status = 404, Title = "El archivo no está disponible" });
        return File(stream, attachment.ContentType, attachment.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("api/v1/adjuntos/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.Include(x => x.Task)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (attachment is null) return NotFound();
        var role = await access.GetRoleAsync(attachment.Task.ProjectId, User.GetUserId(), cancellationToken);
        if (role is null) return NotFound();
        if (role == ProjectRole.Viewer) return Forbid();
        await storage.DeleteAsync(attachment.RelativePath, cancellationToken);
        db.Attachments.Remove(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static async Task<bool> HasValidSignature(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        return file.ContentType.ToLowerInvariant() switch
        {
            "application/pdf" => bytesRead >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            "image/jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/gif" => bytesRead >= 6 && (header.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || header.AsSpan(0, 6).SequenceEqual("GIF89a"u8)),
            "image/webp" => bytesRead >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
