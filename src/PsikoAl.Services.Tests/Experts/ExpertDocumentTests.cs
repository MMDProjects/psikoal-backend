using System.Text;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Expert.Create;
using PsikoAl.Common.Dtos.Expert.Update;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Services.Tests.Support;

namespace PsikoAl.Services.Tests.Experts;

// Admin belge ucu yolları service_role ile imzalıyor. Belge yolu yalnız uzmanın kendi
// klasöründe olabilir ve yalnız yükleme uçlarından yazılabilir.
public sealed class ExpertDocumentTests
{
    private static readonly Guid ExpertId = Guid.Parse("3f1b2c4d-0000-0000-0000-000000000001");

    [Theory]
    [InlineData("3f1b2c4d-0000-0000-0000-000000000001/cv.pdf", true)]
    [InlineData("3f1b2c4d-0000-0000-0000-000000000001/certificates/a.pdf", true)]
    [InlineData("3f1b2c4d-0000-0000-0000-000000000002/cv.pdf", false)]
    [InlineData("3f1b2c4d-0000-0000-0000-000000000001/../3f1b2c4d-0000-0000-0000-000000000002/cv.pdf", false)]
    [InlineData("3f1b2c4d-0000-0000-0000-000000000001/", false)]
    [InlineData("3f1b2c4d-0000-0000-0000-000000000001//cv.pdf", false)]
    [InlineData("BDT Uygulayıcı Sertifikası", false)]
    [InlineData("https://cdn.example.test/3f1b2c4d-0000-0000-0000-000000000001/cv.pdf", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_paths_inside_the_experts_folder_are_signable(string? path, bool expected)
        => Assert.Equal(expected, ExpertDocumentPaths.IsOwnedBy(ExpertId, path));

    [Fact]
    public async Task Profile_create_ignores_client_supplied_document_fields()
    {
        using var database = new TestDatabase();
        var profile = await database.AddProfileAsync(role: ProfileRoles.Expert);
        var service = new ExpertService(database.UnitOfWork, new AllCategoriesExistStub());

        await service.CreateProfileAsync(
            profile.Id,
            new CreateExpertProfileDto(
                "Klinik Psikolog", ["Kaygı"], 5, new string('a', 60), AvatarUrl: null, Education: null,
                CvUrl: $"{Guid.NewGuid()}/cv.pdf", Certificates: ["başkasının/belgesi.pdf"], PersonalWebsite: null),
            CancellationToken.None);

        var expert = Assert.Single(database.Context.Experts);
        Assert.Null(expert.CvUrl);
        Assert.Empty(expert.Certificates);
    }

    [Fact]
    public void Profile_update_ignores_client_supplied_document_fields()
    {
        var expert = new Expert { Id = ExpertId, Title = "Psikolog", CvUrl = $"{ExpertId}/cv.pdf", Certificates = [$"{ExpertId}/certificates/a.pdf"] };

        ExpertService.ApplyUpdate(expert, new UpdateExpertProfileDto(
            Title: null, Specializations: null, ExperienceYears: null, Bio: null, Education: null,
            CvUrl: "uydurma/cv.pdf", Certificates: [], PersonalWebsite: null));

        Assert.Equal($"{ExpertId}/cv.pdf", expert.CvUrl);
        Assert.Single(expert.Certificates);
    }

    [Fact]
    public async Task Certificate_upload_is_capped()
    {
        using var database = new TestDatabase();
        var profile = await database.AddProfileAsync(role: ProfileRoles.Expert);
        database.Context.Experts.Add(new Expert
        {
            Id = profile.Id,
            Title = "Psikolog",
            Certificates = [.. Enumerable.Range(0, ExpertDocumentPaths.MaxCertificates).Select(index => $"{profile.Id}/certificates/{index}.pdf")],
        });
        await database.Context.SaveChangesAsync();
        var service = new UploadService(database.UnitOfWork, new FakeStorageService());
        await using var pdf = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7 test"));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UploadCertificateAsync(profile.Id, pdf, "application/pdf", CancellationToken.None));

        Assert.Equal(ErrorKeys.ExpertCertificateLimitExceeded, exception.ErrorKey);
    }
}
