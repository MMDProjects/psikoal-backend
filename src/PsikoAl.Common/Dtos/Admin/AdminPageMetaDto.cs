namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminPageMetaDto(int Page, int PerPage, int Total)
{
    public int TotalPages => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)PerPage);
}
