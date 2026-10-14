namespace PsikoAl.Common.Dtos.Admin;

public sealed record UpdateAdminCreditPackageDto(string Name, int Credits, decimal PriceTry, bool IsActive, int SortOrder);
