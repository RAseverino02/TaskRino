using GestorTareas.Api.Data;
using GestorTareas.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Tests;

public sealed class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_DoesNotOverwriteExistingDemoUserProfile()
    {
        await using var db = CreateDatabase();
        var hasher = new PasswordHasher<User>();
        await DbSeeder.SeedAsync(db, hasher);

        var severino = await db.Users.SingleAsync(user => user.Email == "severino@rino.com");
        severino.Name = "Nombre personalizado";
        await db.SaveChangesAsync();
        var passwordHash = severino.PasswordHash;

        await DbSeeder.SeedAsync(db, hasher);

        var persisted = await db.Users.AsNoTracking().SingleAsync(user => user.Email == "severino@rino.com");
        Assert.Equal("Nombre personalizado", persisted.Name);
        Assert.Equal(passwordHash, persisted.PasswordHash);
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }
}
