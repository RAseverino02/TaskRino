using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestorTareas.Api.Models;
using GestorTareas.Api.Services;
using Microsoft.Extensions.Options;

namespace GestorTareas.Api.Tests;

public sealed class TokenServiceTests
{
    private readonly TokenService _service = new(Options.Create(new JwtOptions
    {
        Issuer = "tests", Audience = "tests-spa",
        Key = "una-clave-de-pruebas-segura-con-mas-de-32-bytes-1234567890",
        AccessTokenMinutes = 15
    }));

    [Fact]
    public void CreateAccessToken_IncludesUserIdentityAndExpiration()
    {
        var user = new User { Name = "Ana", Email = "ana@test.com", PasswordHash = "hash" };

        var (token, expiresAt) = _service.CreateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("ana@test.com", jwt.Claims.Single(x => x.Type == ClaimTypes.Email).Value);
        Assert.InRange(expiresAt, DateTimeOffset.UtcNow.AddMinutes(14), DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void RefreshTokens_AreRandomAndStoredAsDeterministicHashes()
    {
        var first = _service.CreateRefreshToken();
        var second = _service.CreateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(_service.HashToken(first), _service.HashToken(first));
        Assert.NotEqual(first, _service.HashToken(first));
        Assert.Equal(64, _service.HashToken(first).Length);
    }
}
