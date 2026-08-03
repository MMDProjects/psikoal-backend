namespace PsikoAl.Common.Dtos.Expert.Create;

public sealed record CreateExpertProfileDto(
    string Title,
    IReadOnlyList<string> Specializations,
    int ExperienceYears,
    string Bio,
    string? AvatarUrl,
    string? Education,
    string? CvUrl,
    IReadOnlyList<string>? Certificates,
    string? PersonalWebsite);
