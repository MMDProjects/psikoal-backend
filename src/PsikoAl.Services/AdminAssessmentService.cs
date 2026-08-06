using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class AdminAssessmentService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : IAdminAssessmentService
{
    public async Task<IReadOnlyList<AdminAssessmentListItemDto>> ListAsync(CancellationToken cancellationToken)
    {
        var assessments = await unitOfWork.Assessments.Query()
            .OrderBy(assessment => assessment.SortOrder)
            .ToListAsync(cancellationToken);

        var counts = await unitOfWork.AssessmentQuestions.Query()
            .GroupBy(question => question.AssessmentId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Key, row => row.Count, cancellationToken);

        return [.. assessments.Select(assessment => new AdminAssessmentListItemDto(
            assessment.Id,
            assessment.Title,
            assessment.Category,
            assessment.EstimatedMinutes,
            counts.GetValueOrDefault(assessment.Id),
            assessment.IsActive,
            assessment.SortOrder))];
    }

    public async Task<AdminAssessmentDetailDto> GetDetailAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var assessment = await GetRequiredAssessmentAsync(assessmentId, cancellationToken);
        return await ToDetailDtoAsync(assessment, cancellationToken);
    }

    public async Task<AdminAssessmentDetailDto> UpdateAsync(
        Guid actorAuthUserId,
        Guid assessmentId,
        UpdateAdminAssessmentDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var assessment = await GetRequiredAssessmentAsync(assessmentId, cancellationToken);
        var oldSnapshot = JsonSerializer.Serialize(new { assessment.Title, assessment.IsActive });

        assessment.Title = request.Title;
        assessment.Description = request.Description;
        assessment.Category = request.Category;
        assessment.EstimatedMinutes = request.EstimatedMinutes;
        assessment.IsActive = request.IsActive;
        assessment.SortOrder = request.SortOrder;

        await AddAuditAsync(
            actor.Id, "admin.assessment_update", assessment.Id,
            oldSnapshot, JsonSerializer.Serialize(new { assessment.Title, assessment.IsActive }), null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDetailDtoAsync(assessment, cancellationToken);
    }

    public async Task<AdminScoreRuleDto> UpdateScoreRuleAsync(
        Guid actorAuthUserId,
        Guid scoreRuleId,
        UpdateAdminScoreRuleDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var rule = await unitOfWork.AssessmentScoreRules.GetByIdAsync(scoreRuleId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AssessmentScoreRuleNotFound);

        var oldSnapshot = JsonSerializer.Serialize(new { rule.Summary });
        rule.Summary = request.Summary;
        rule.Suggestions = [.. request.Suggestions];

        await AddAuditAsync(
            actor.Id, "admin.assessment_score_rule_update", rule.Id,
            oldSnapshot, JsonSerializer.Serialize(new { rule.Summary }), null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminScoreRuleDto(rule.Id, rule.MinScore, rule.MaxScore, rule.Level, rule.Summary, rule.Suggestions);
    }

    private async Task<AdminAssessmentDetailDto> ToDetailDtoAsync(Assessment assessment, CancellationToken cancellationToken)
    {
        var questions = await unitOfWork.AssessmentQuestions.ListForAssessmentAsync(assessment.Id, cancellationToken);
        var rules = await unitOfWork.AssessmentScoreRules.ListForAssessmentAsync(assessment.Id, cancellationToken);

        return new AdminAssessmentDetailDto(
            assessment.Id,
            assessment.Title,
            assessment.Description,
            assessment.Category,
            assessment.EstimatedMinutes,
            assessment.IsActive,
            assessment.SortOrder,
            [.. questions.Select(AssessmentMapper.ToQuestionDto)],
            [.. rules.Select(rule => new AdminScoreRuleDto(rule.Id, rule.MinScore, rule.MaxScore, rule.Level, rule.Summary, rule.Suggestions))]);
    }

    private async Task<AdminUser> GetRequiredActorAsync(Guid actorAuthUserId, CancellationToken cancellationToken)
        => await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

    private async Task<Assessment> GetRequiredAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken)
        => await unitOfWork.Assessments.GetByIdAsync(assessmentId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AssessmentNotFound);

    private async Task AddAuditAsync(
        Guid adminUserId,
        string action,
        Guid entityId,
        string oldValue,
        string newValue,
        string? reason,
        CancellationToken cancellationToken)
        => await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = adminUserId,
                ActorType = AuditActorTypes.Admin,
                Action = action,
                EntityType = "assessment",
                EntityId = entityId.ToString(),
                OldValue = oldValue,
                NewValue = newValue,
                Reason = reason,
            },
            cancellationToken);
}
