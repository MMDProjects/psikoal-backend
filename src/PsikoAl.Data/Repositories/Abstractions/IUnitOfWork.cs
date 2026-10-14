namespace PsikoAl.Data.Repositories.Abstractions;

public interface IUnitOfWork
{
    IProfileRepository Profiles { get; }

    IExpertRepository Experts { get; }

    ICategoryRepository Categories { get; }

    IReviewRepository Reviews { get; }

    IListingRepository Listings { get; }

    IOfferRepository Offers { get; }

    IMatchRepository Matches { get; }

    INotificationTemplateRepository NotificationTemplates { get; }

    INotificationRepository Notifications { get; }

    IPushTokenRepository PushTokens { get; }

    IAssessmentRepository Assessments { get; }

    IAssessmentQuestionRepository AssessmentQuestions { get; }

    IAssessmentScoreRuleRepository AssessmentScoreRules { get; }

    IAssessmentResultRepository AssessmentResults { get; }

    ISystemSettingRepository SystemSettings { get; }

    IAdminUserRepository AdminUsers { get; }

    IAuditLogRepository AuditLogs { get; }

    IBlogRepository Blogs { get; }

    IBlogLikeRepository BlogLikes { get; }

    ICreditPackageRepository CreditPackages { get; }

    ICreditPaymentRepository CreditPayments { get; }

    ICreditTransactionRepository CreditTransactions { get; }

    IExpertCreditRepository ExpertCredits { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
