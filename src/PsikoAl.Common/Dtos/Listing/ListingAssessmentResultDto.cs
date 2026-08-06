namespace PsikoAl.Common.Dtos.Listing;

public sealed record ListingAssessmentResultDto(Guid Id, int Score, string Level, string Summary, string AssessmentTitle);
