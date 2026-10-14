using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Tests.Admin;

public sealed class AdminPagingTests
{
    [Theory]
    [InlineData(null, null, 1, AdminPaging.DefaultPerPage)]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, 500, 1, AdminPaging.MaxPerPage)]
    [InlineData(4, 50, 4, 50)]
    public void Normalize_ClampsPageAndPerPage(int? page, int? perPage, int expectedPage, int expectedPerPage)
    {
        var meta = AdminPaging.Normalize(page, perPage);

        Assert.Equal(expectedPage, meta.Page);
        Assert.Equal(expectedPerPage, meta.PerPage);
    }

    [Theory]
    [InlineData(0, 25, 1)]
    [InlineData(25, 25, 1)]
    [InlineData(26, 25, 2)]
    [InlineData(101, 100, 2)]
    public void TotalPages_RoundsUpAndNeverDropsBelowOne(int total, int perPage, int expectedPages)
    {
        Assert.Equal(expectedPages, new AdminPageMetaDto(1, perPage, total).TotalPages);
    }
}
