using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Backend.Models;

namespace Backend.Data;

public class Db : IdentityDbContext<User>
{
    public Db(DbContextOptions<Db> options) : base(options)
    {
    }

    public DbSet<AutoEmail> AutoEmails => Set<AutoEmail>();

    public DbSet<Circle> Circles => Set<Circle>();

    public DbSet<CircleRole> CircleRoles => Set<CircleRole>();

    public DbSet<UserCircleRole> UserCircleRoles => Set<UserCircleRole>();

    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CircleRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(role => role.Name).HasMaxLength(32);
            entity.HasIndex(role => role.Name).IsUnique();
            entity.HasData(
                new CircleRole { Id = 1, Name = CircleRole.Owner },
                new CircleRole { Id = 2, Name = CircleRole.Moderator },
                new CircleRole { Id = 3, Name = CircleRole.Member }
            );
        });

        modelBuilder.Entity<Circle>(entity =>
        {
            entity.ToTable("Circles");
            entity.Property(circle => circle.Name).HasMaxLength(200);
            entity.HasOne(circle => circle.CreatedByUser)
                .WithMany()
                .HasForeignKey(circle => circle.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserCircleRole>(entity =>
        {
            entity.ToTable("UserCircleRoles");
            entity.HasKey(membership => new { membership.UserId, membership.CircleId });
            entity.HasOne(membership => membership.User)
                .WithMany()
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(membership => membership.Circle)
                .WithMany(circle => circle.Members)
                .HasForeignKey(membership => membership.CircleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(membership => membership.Role)
                .WithMany()
                .HasForeignKey(membership => membership.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasOne(post => post.Circle)
                .WithMany(circle => circle.Posts)
                .HasForeignKey(post => post.CircleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(post => post.User)
                .WithMany()
                .HasForeignKey(post => post.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
