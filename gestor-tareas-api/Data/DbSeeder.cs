using GestorTareas.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> hasher)
    {
        var severino = await db.Users.SingleOrDefaultAsync(user => user.Email == "severino@rino.com");
        if (severino is null)
        {
            severino = await db.Users.SingleOrDefaultAsync(user => user.Email == "ana@demo.com");
            if (severino is null)
            {
                severino = new User { Name = "Severino Rino", Email = "severino@rino.com", PasswordHash = string.Empty };
                db.Users.Add(severino);
            }

            // Inicializa o migra la cuenta de demostración una sola vez.
            // Si ya existe, sus cambios de perfil y contraseña deben conservarse.
            severino.Name = "Severino Rino";
            severino.Email = "severino@rino.com";
            severino.PasswordHash = hasher.HashPassword(severino, "S3ver1n0");
        }

        var richard = await db.Users.SingleOrDefaultAsync(user => user.Email == "richard@rino.com");
        if (richard is null)
        {
            richard = await db.Users.SingleOrDefaultAsync(user => user.Email == "luis@demo.com");
            if (richard is null)
            {
                richard = new User { Name = "Richard Rino", Email = "richard@rino.com", PasswordHash = string.Empty };
                db.Users.Add(richard);
            }

            richard.Name = "Richard Rino";
            richard.Email = "richard@rino.com";
            richard.PasswordHash = hasher.HashPassword(richard, "R1ch@rd");
        }

        await db.SaveChangesAsync();

        if (await db.Projects.AnyAsync()) return;

        var lanzamiento = new Project
        {
            Name = "Lanzamiento del producto",
            Description = "Plan de trabajo para publicar la primera versión.",
            Color = "#1C6E72",
            OwnerId = severino.Id
        };
        var sitio = new Project
        {
            Name = "Rediseño del sitio",
            Description = "Mejoras de experiencia, contenido y rendimiento.",
            Color = "#2F5DA8",
            OwnerId = richard.Id
        };

        lanzamiento.Members.Add(new ProjectMember { ProjectId = lanzamiento.Id, UserId = severino.Id, Role = ProjectRole.Owner });
        lanzamiento.Members.Add(new ProjectMember { ProjectId = lanzamiento.Id, UserId = richard.Id, Role = ProjectRole.Editor });
        sitio.Members.Add(new ProjectMember { ProjectId = sitio.Id, UserId = richard.Id, Role = ProjectRole.Owner });
        sitio.Members.Add(new ProjectMember { ProjectId = sitio.Id, UserId = severino.Id, Role = ProjectRole.Viewer });

        lanzamiento.Tasks.Add(new WorkItem { Title = "Definir alcance del MVP", Priority = PriorityLevel.Alta, Status = WorkItemStatus.Done, AssignedToId = severino.Id });
        lanzamiento.Tasks.Add(new WorkItem { Title = "Preparar campaña de lanzamiento", Priority = PriorityLevel.Alta, Status = WorkItemStatus.InProgress, AssignedToId = richard.Id, DueDate = DateTimeOffset.UtcNow.AddDays(5) });
        lanzamiento.Tasks.Add(new WorkItem { Title = "Revisar métricas iniciales", Priority = PriorityLevel.Media, Status = WorkItemStatus.ToDo, AssignedToId = severino.Id, DueDate = DateTimeOffset.UtcNow.AddDays(12) });
        sitio.Tasks.Add(new WorkItem { Title = "Auditar navegación actual", Priority = PriorityLevel.Media, Status = WorkItemStatus.Done, AssignedToId = severino.Id });
        sitio.Tasks.Add(new WorkItem { Title = "Construir prototipo responsive", Priority = PriorityLevel.Alta, Status = WorkItemStatus.InProgress, AssignedToId = richard.Id, DueDate = DateTimeOffset.UtcNow.AddDays(8) });

        db.Projects.AddRange(lanzamiento, sitio);
        await db.SaveChangesAsync();
    }
}
