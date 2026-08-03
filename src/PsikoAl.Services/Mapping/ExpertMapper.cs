using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Expert;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class ExpertMapper
{
    public static ExpertDto ToExpertDto(Expert expert, Profile profile, double rating, int reviewCount)
        => new(
            expert.Id,
            profile.FirstName,
            profile.LastName,
            expert.Title,
            expert.Specializations,
            expert.ExperienceYears,
            expert.Bio,
            profile.AvatarUrl,
            rating,
            reviewCount,
            profile.IsVerified,
            expert.Status,
            InitialsOf(profile.FirstName, profile.LastName),
            expert.Status == ExpertStatuses.Approved,
            expert.Education,
            expert.CvUrl,
            expert.Certificates,
            expert.PersonalWebsite);

    public static string InitialsOf(string firstName, string lastName)
    {
        var first = firstName.Length > 0 ? char.ToUpperInvariant(firstName[0]).ToString() : string.Empty;
        var last = lastName.Length > 0 ? char.ToUpperInvariant(lastName[0]).ToString() : string.Empty;
        return first + last;
    }
}
