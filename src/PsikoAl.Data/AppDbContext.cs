using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;

namespace PsikoAl.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Expert> Experts => Set<Expert>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<ExpertRating> ExpertRatings => Set<ExpertRating>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Expert>(entity =>
        {
            entity.ToTable("experts");
            entity.HasKey(expert => expert.Id);
            entity.Property(expert => expert.Id).ValueGeneratedNever();
            entity.Property(expert => expert.PendingRevision).HasColumnType("jsonb");
            entity.Property(expert => expert.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(expert => expert.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(expert => expert.Profile)
                .WithOne()
                .HasForeignKey<Expert>(expert => expert.Id);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(category => category.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(review => review.Id);
            entity.Property(review => review.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(review => review.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(review => review.Client)
                .WithMany()
                .HasForeignKey(review => review.ClientId);
        });

        modelBuilder.Entity<ExpertRating>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("expert_ratings");
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(adminUser => adminUser.Id);
            entity.HasIndex(adminUser => adminUser.AuthUserId).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(auditLog => auditLog.Id);
            entity.Property(auditLog => auditLog.OldValue).HasColumnType("jsonb");
            entity.Property(auditLog => auditLog.NewValue).HasColumnType("jsonb");
        });
    }
}
