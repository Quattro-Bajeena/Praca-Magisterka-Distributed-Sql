using Microsoft.EntityFrameworkCore;
using NSCI.Data.Entities;

namespace NSCI.Data;

public class NsciDbContext : DbContext
{
    public DbSet<DatabaseEntity> Databases { get; set; }
    public DbSet<TestResultEntity> TestResults { get; set; }

    public NsciDbContext(DbContextOptions<NsciDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DatabaseEntity>(entity =>
        {
            entity.ToTable("databases");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DatabaseId).HasColumnName("database_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Product).HasColumnName("product").HasMaxLength(100);
            entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(50);
            entity.Property(e => e.ReleaseYear).HasColumnName("release_year");
            entity.Property(e => e.Result).HasColumnName("result").HasPrecision(5, 4);
            entity.HasIndex(e => e.DatabaseId).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<TestResultEntity>(entity =>
        {
            entity.ToTable("test_results");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DatabaseId).HasColumnName("database_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ClassName).HasColumnName("class_name").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Passed).HasColumnName("passed");
            entity.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(50);
            entity.Property(e => e.Error).HasColumnName("error");
            entity.Property(e => e.FailureCategory).HasColumnName("failure_category").HasConversion<int?>();

            entity.HasIndex(e => new { e.DatabaseId, e.Name }).IsUnique();

            entity.HasOne(e => e.Database)
                  .WithMany(d => d.TestResults)
                  .HasForeignKey(e => e.DatabaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
