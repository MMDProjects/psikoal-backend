namespace PsikoAl.Common.Dtos.Match;

public sealed record MatchListResult(IReadOnlyList<MatchDto> Data, int Total, int ActiveCount, int PastCount);
