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

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PushToken> PushTokens => Set<PushToken>();

    public DbSet<Assessment> Assessments => Set<Assessment>();

    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();

    public DbSet<AssessmentScoreRule> AssessmentScoreRules => Set<AssessmentScoreRule>();

    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

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
            entity.HasOne<Match>()
                .WithMany()
                .HasForeignKey(review => review.MatchId);
        });

        modelBuilder.Entity<ExpertRating>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("expert_ratings");
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("listings");
            entity.HasKey(listing => listing.Id);
            entity.Property(listing => listing.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(listing => listing.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(listing => listing.Client)
                .WithMany()
                .HasForeignKey(listing => listing.ClientId);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("offers");
            entity.HasKey(offer => offer.Id);
            entity.Property(offer => offer.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(offer => offer.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(offer => offer.Listing)
                .WithMany()
                .HasForeignKey(offer => offer.ListingId);
            entity.HasOne(offer => offer.Expert)
                .WithMany()
                .HasForeignKey(offer => offer.ExpertId);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(match => match.Id);
            entity.Property(match => match.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(match => match.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(match => match.Listing)
                .WithMany()
                .HasForeignKey(match => match.ListingId);
            entity.HasOne(match => match.AcceptedOffer)
                .WithMany()
                .HasForeignKey(match => match.AcceptedOfferId);
            entity.HasOne(match => match.Client)
                .WithMany()
                .HasForeignKey(match => match.ClientId);
            entity.HasOne(match => match.Expert)
                .WithMany()
                .HasForeignKey(match => match.ExpertId);
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.ToTable("notification_templates");
            entity.HasKey(template => template.Id);
            entity.Property(template => template.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(template => template.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Data).HasColumnType("jsonb");
            entity.Property(notification => notification.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<PushToken>(entity =>
        {
            entity.ToTable("push_tokens");
            entity.HasKey(pushToken => pushToken.Id);
            entity.HasIndex(pushToken => pushToken.Token).IsUnique();
            entity.Property(pushToken => pushToken.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("assessments");
            entity.HasKey(assessment => assessment.Id);
            entity.Property(assessment => assessment.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(assessment => assessment.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<AssessmentQuestion>(entity =>
        {
            entity.ToTable("assessment_questions");
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Options).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AssessmentScoreRule>(entity =>
        {
            entity.ToTable("assessment_score_rules");
            entity.HasKey(rule => rule.Id);
        });

        modelBuilder.Entity<AssessmentResult>(entity =>
        {
            entity.ToTable("assessment_results");
            entity.HasKey(result => result.Id);
            entity.Property(result => result.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.HasOne(result => result.Assessment)
                .WithMany()
                .HasForeignKey(result => result.AssessmentId);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(setting => setting.Key);
            entity.Property(setting => setting.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
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
