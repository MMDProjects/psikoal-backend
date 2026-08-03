using PsikoAl.Common.Dtos.Expert;
using PsikoAl.Common.Dtos.Expert.Update;

namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminExpertDetailDto(
    ExpertDto Current,
    string Email,
    string? RejectionReason,
    DateTimeOffset? ApprovedAt,
    UpdateExpertProfileDto? PendingRevision);
