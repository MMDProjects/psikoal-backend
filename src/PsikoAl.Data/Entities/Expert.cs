using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class Expert
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public List<string> Specializations { get; set; } = [];

    public int ExperienceYears { get; set; }

    public string Bio { get; set; } = string.Empty;

    public string? Education { get; set; }

    public string? CvUrl { get; set; }

    public List<string> Certificates { get; set; } = [];

    public string? PersonalWebsite { get; set; }

    public string Status { get; set; } = ExpertStatuses.Pending;

    public string? RejectionReason { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public Guid? ApprovedBy { get; set; }

    public string? PendingRevision { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Profile? Profile { get; set; }
}
