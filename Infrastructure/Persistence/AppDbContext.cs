using ComponentesIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ExtractionTemplate> ExtractionTemplates { get; set; }
    public DbSet<ExtractionField> ExtractionFields { get; set; }
    public DbSet<DocumentBatch> DocumentBatches { get; set; }
    public DbSet<DocumentFile> DocumentFiles { get; set; }
    public DbSet<ExtractionJob> ExtractionJobs { get; set; }
    public DbSet<ExtractionResult> ExtractionResults { get; set; }
    public DbSet<ExtractionFieldResult> ExtractionFieldResults { get; set; }

    // Auth/Identity
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- Auth/Identity ---
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.UserName).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
                e.Property(x => x.Email).HasMaxLength(200).IsRequired();
                e.Property(x => x.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<Role>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            });

            modelBuilder.Entity<Permission>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            });

            modelBuilder.Entity<UserRole>(e =>
            {
                e.HasKey(x => new { x.UserId, x.RoleId });
                e.HasOne(x => x.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(x => x.UserId);
                e.HasOne(x => x.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(x => x.RoleId);
            });

            modelBuilder.Entity<RolePermission>(e =>
            {
                e.HasKey(x => new { x.RoleId, x.PermissionId });
                e.HasOne(x => x.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(x => x.RoleId);
                e.HasOne(x => x.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(x => x.PermissionId);
            });

            base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExtractionTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<ExtractionField>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.DataType).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Template)
             .WithMany(t => t.Fields)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Template)
             .WithMany(t => t.Batches)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.OriginalFileName).HasMaxLength(500);
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.HasOne(x => x.Batch)
             .WithMany(b => b.Documents)
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExtractionJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.DocumentFile)
             .WithOne(f => f.Job)
             .HasForeignKey<ExtractionJob>(j => j.DocumentFileId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Template)
             .WithMany()
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExtractionResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Job)
             .WithOne(j => j.Result)
             .HasForeignKey<ExtractionResult>(r => r.JobId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExtractionFieldResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Result)
             .WithMany(r => r.FieldResults)
             .HasForeignKey(x => x.ResultId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
