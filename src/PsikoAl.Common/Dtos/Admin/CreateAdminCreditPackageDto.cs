namespace PsikoAl.Common.Dtos.Admin;

public sealed record CreateAdminCreditPackageDto(string Name, int Credits, decimal PriceTry, bool IsActive, int SortOrder);
