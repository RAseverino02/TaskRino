using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Models;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Conflict(new ProblemDetails { Title = "El correo ya está registrado", Status = 409 });

        var user = new User { Name = request.Name.Trim(), Email = email, PasswordHash = string.Empty };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Usuario {UserId} registrado", user.Id);
        var response = await IssueTokens(user, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new ProblemDetails { Title = "Credenciales incorrectas", Detail = "Revisa tu correo y contraseña.", Status = 401 });

        return Ok(await IssueTokens(user, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored is null || !stored.IsActive)
            return Unauthorized(new ProblemDetails { Title = "Sesión expirada", Detail = "Inicia sesión nuevamente.", Status = 401 });

        var newRawToken = tokenService.CreateRefreshToken();
        var newHash = tokenService.HashToken(newRawToken);
        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenHash = newHash;
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = stored.UserId, TokenHash = newHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        });
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(stored.User);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new AuthResponse(accessToken, newRawToken, expiresAt, ToDto(stored.User)));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    private async Task<AuthResponse> IssueTokens(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
        var rawRefreshToken = tokenService.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id, TokenHash = tokenService.HashToken(rawRefreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        });
        await db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(accessToken, rawRefreshToken, expiresAt, ToDto(user));
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Name, user.Email, user.RegisteredAt, user.GetProfileImageUrl());
}
