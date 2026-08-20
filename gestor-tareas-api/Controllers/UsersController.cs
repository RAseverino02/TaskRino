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
[Route("api/v1/usuarios")]
public sealed class UsersController(AppDbContext db, IAttachmentStorage storage) : ControllerBase
{
    private const long MaxProfileImageBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedProfileImageTypes = ["image/jpeg", "image/png", "image/webp"];

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == id, cancellationToken);
        return Ok(ToDto(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        var user = await db.Users.SingleAsync(x => x.Id == id, cancellationToken);
        user.Name = request.Name.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(user));
    }

    [HttpPost("me/foto")]
    [RequestSizeLimit(MaxProfileImageBytes + 64 * 1024)]
    public async Task<ActionResult<UserDto>> UploadProfileImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > MaxProfileImageBytes)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Imagen inválida", Detail = "La foto debe pesar como máximo 2 MB." });
        if (!AllowedProfileImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Formato no permitido", Detail = "Utiliza una imagen JPG, PNG o WebP." });

        var user = await db.Users.SingleAsync(x => x.Id == User.GetUserId(), cancellationToken);
        var previousPath = user.ProfileImagePath;
        var newPath = await storage.SaveAsync(user.Id, file, cancellationToken);
        user.ProfileImagePath = newPath;
        user.ProfileImageContentType = file.ContentType.ToLowerInvariant();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(newPath, cancellationToken);
            throw;
        }
        if (previousPath is not null) await storage.DeleteAsync(previousPath, cancellationToken);
        return Ok(ToDto(user));
    }

    [HttpDelete("me/foto")]
    public async Task<ActionResult<UserDto>> DeleteProfileImage(CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(x => x.Id == User.GetUserId(), cancellationToken);
        var previousPath = user.ProfileImagePath;
        user.ProfileImagePath = null;
        user.ProfileImageContentType = null;
        await db.SaveChangesAsync(cancellationToken);
        if (previousPath is not null) await storage.DeleteAsync(previousPath, cancellationToken);
        return Ok(ToDto(user));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/foto")]
    public async Task<IActionResult> GetProfileImage(Guid id, CancellationToken cancellationToken)
    {
        var image = await db.Users.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.ProfileImagePath, x.ProfileImageContentType })
            .SingleOrDefaultAsync(cancellationToken);
        if (image?.ProfileImagePath is null || image.ProfileImageContentType is null) return NotFound();
        var stream = await storage.OpenReadAsync(image.ProfileImagePath, cancellationToken);
        if (stream is null) return NotFound();
        Response.Headers.CacheControl = "public,max-age=86400";
        return File(stream, image.ProfileImageContentType);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Name, user.Email, user.RegisteredAt, user.GetProfileImageUrl());
}
