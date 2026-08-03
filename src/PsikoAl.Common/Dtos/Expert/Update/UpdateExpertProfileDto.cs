namespace PsikoAl.Common.Dtos.Expert.Update;

public sealed record UpdateExpertProfileDto(
    string? Title,
    IReadOnlyList<string>? Specializations,
    int? ExperienceYears,
    string? Bio,
    string? Education,
    string? CvUrl,
    IReadOnlyList<string>? Certificates,
    string? PersonalWebsite);
