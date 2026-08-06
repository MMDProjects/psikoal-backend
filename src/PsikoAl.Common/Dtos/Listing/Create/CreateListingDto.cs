namespace PsikoAl.Common.Dtos.Listing.Create;

public sealed record CreateListingDto(
    string Title,
    string? Description,
    IReadOnlyList<string> Specialization,
    decimal BudgetMin,
    decimal BudgetMax,
    string PreferredSessionType,
    string? City,
    Guid? AssessmentResultId);
