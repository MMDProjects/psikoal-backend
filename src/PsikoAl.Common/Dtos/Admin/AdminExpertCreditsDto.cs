namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminExpertCreditsDto(
    Guid ExpertId,
    string FullName,
    int Balance,
    AdminPagedResultDto<AdminCreditTransactionDto> Transactions);
