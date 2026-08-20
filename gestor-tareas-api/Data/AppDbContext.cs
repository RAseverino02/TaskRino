using GestorTareas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<WorkItem> Tasks => Set<WorkItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        builder.Entity<ProjectMember>().HasKey(x => new { x.ProjectId, x.UserId });

        builder.Entity<Project>()
            .HasOne(x => x.Owner).WithMany(x => x.OwnedProjects)
            .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProjectMember>()
            .HasOne(x => x.Project).WithMany(x => x.Members)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ProjectMember>()
            .HasOne(x => x.User).WithMany(x => x.ProjectMemberships)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WorkItem>()
            .HasOne(x => x.Project).WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkItem>()
            .HasOne(x => x.AssignedTo).WithMany()
            .HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Comment>()
            .HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RefreshToken>()
            .HasOne(x => x.User).WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Project>().Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Entity<WorkItem>().Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Entity<WorkItem>().Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Entity<WorkItem>().Property(x => x.DueDate).HasColumnType("timestamp with time zone");
    }
}
