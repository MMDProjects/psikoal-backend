using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;

namespace PsikoAl.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();

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
