using ComponentesIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ExtractionTemplate> ExtractionTemplates => Set<ExtractionTemplate>();
    public DbSet<ExtractionField> ExtractionFields => Set<ExtractionField>();
    public DbSet<DocumentBatch> DocumentBatches => Set<DocumentBatch>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<ExtractionJob> ExtractionJobs => Set<ExtractionJob>();
    public DbSet<ExtractionResult> ExtractionResults => Set<ExtractionResult>();
    public DbSet<ExtractionFieldResult> ExtractionFieldResults => Set<ExtractionFieldResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
