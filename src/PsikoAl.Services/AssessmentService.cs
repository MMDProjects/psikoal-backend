using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Assessment;
using PsikoAl.Common.Dtos.Assessment.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class AssessmentService(IUnitOfWork unitOfWork) : IAssessmentService
{
    public async Task<IReadOnlyList<AssessmentListItemDto>> ListAsync(string? category, CancellationToken cancellationToken)
    {
        var query = unitOfWork.Assessments.Query().Where(assessment => assessment.IsActive);
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(assessment => assessment.Category == category);
        }

        var assessments = await query.OrderBy(assessment => assessment.SortOrder).ToListAsync(cancellationToken);
        var assessmentIds = assessments.Select(assessment => assessment.Id).ToList();

        var counts = await unitOfWork.AssessmentQuestions.Query()
            .Where(question => assessmentIds.Contains(question.AssessmentId))
            .GroupBy(question => question.AssessmentId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Key, row => row.Count, cancellationToken);

        return [.. assessments.Select(assessment => new AssessmentListItemDto(
            assessment.Id,
            assessment.Title,
            assessment.Category,
            assessment.EstimatedMinutes,
            counts.GetValueOrDefault(assessment.Id)))];
    }

    public async Task<AssessmentDto?> GetActiveAsync(CancellationToken cancellationToken)
    {
        var assessment = await unitOfWork.Assessments.Query()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        var questions = await unitOfWork.AssessmentQuestions.ListForAssessmentAsync(assessment.Id, cancellationToken);
        return AssessmentMapper.ToAssessmentDto(assessment, questions);
    }

    public async Task<AssessmentResultDto> SubmitAsync(Guid? userId, SubmitAssessmentDto request, CancellationToken cancellationToken)
    {
        var assessment = await unitOfWork.Assessments.GetByIdAsync(request.AssessmentId, cancellationToken);
        if (assessment is null || !assessment.IsActive)
        {
            throw new DomainException(ErrorKeys.AssessmentNotFound);
        }

        var score = request.Answers.Sum(answer => answer.Values.Count > 0 ? answer.Values[0] : 0);
        var rule = await unitOfWork.AssessmentScoreRules.FindMatchingRuleAsync(assessment.Id, score, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AssessmentScoreRuleNotFound);

        var result = new AssessmentResult
        {
            AssessmentId = assessment.Id,
            UserId = userId,
            Score = score,
            Level = rule.Level,
            Summary = rule.Summary,
            Suggestions = [.. rule.Suggestions],
            Email = request.Email,
        };

        await unitOfWork.AssessmentResults.AddAsync(result, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssessmentMapper.ToResultDto(result);
    }

    public async Task<AssessmentResultDto> GetResultAsync(Guid resultId, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.AssessmentResults.GetByIdAsync(resultId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AssessmentResultNotFound);

        return AssessmentMapper.ToResultDto(result);
    }

    public async Task<MyAssessmentResultListResult> ListMyResultsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var results = await unitOfWork.AssessmentResults.QueryWithAssessment()
            .Where(result => result.UserId == userId)
            .OrderByDescending(result => result.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = results.Select(AssessmentMapper.ToMyResultDto).ToList();
        return new MyAssessmentResultListResult(dtos, dtos.Count);
    }
}
