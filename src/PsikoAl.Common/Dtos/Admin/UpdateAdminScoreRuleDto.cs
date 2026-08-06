namespace PsikoAl.Common.Dtos.Admin;

public sealed record UpdateAdminScoreRuleDto(string Summary, IReadOnlyList<string> Suggestions);
