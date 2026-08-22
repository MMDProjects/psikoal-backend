using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/experts")]
[Authorize(Policy = "Admin")]
public sealed class AdminExpertsController(
    IAdminExpertService adminExpertService,
    ISupabaseStorageService storage) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminExpertListItemDto>> List([FromQuery] string? status, CancellationToken cancellationToken)
        => adminExpertService.ListAsync(status, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AdminExpertDetailDto> GetDetail(Guid id, CancellationToken cancellationToken)
        => adminExpertService.GetDetailAsync(id, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await adminExpertService.ApproveAsync(this.CurrentUserId(), id, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionReasonDto body, CancellationToken cancellationToken)
    {
        await adminExpertService.RejectAsync(this.CurrentUserId(), id, body.Reason ?? string.Empty, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> SetVerified(Guid id, [FromBody] AdminSetVerifiedDto body, CancellationToken cancellationToken)
    {
        await adminExpertService.SetVerifiedAsync(this.CurrentUserId(), id, body.IsVerified, cancellationToken);
        return Ok(new { success = true });
    }

    /// <summary>Uzmanın belgeleri için kısa ömürlü imzalı bağlantılar üretir.</summary>
    /// <remarks>
    /// Her belge AYRI AYRI imzalanır ve çözülemeyen ATLANIR. Eskiden tek bir
    /// başarısız imzalama tüm isteği 422'ye düşürüyordu: CV pekâlâ imzalanabilirken
    /// admin HİÇBİR belge göremiyordu.
    ///
    /// Bunun gerçek sebebi <c>certificates</c> alanının çift anlamlı olması:
    /// <c>POST /experts/profile</c> onu serbest metin olarak kabul ediyor
    /// ("BDT Uygulayıcı Sertifikası" gibi), <c>POST /experts/profile/certificates</c>
    /// ise Storage YOLU ekliyor. Aynı dizi ikisini birden tutabiliyor ve metin bir
    /// Storage anahtarı olarak geçersiz (400 InvalidKey).
    ///
    /// Alanı tek anlamlıya indirmek ayrı bir iş (veri göçü gerektirir); bu uç o
    /// karara kadar bozuk veriye rağmen çalışmak zorunda.
    /// </remarks>
    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> GetDocumentUrls(Guid id, CancellationToken cancellationToken)
    {
        var detail = await adminExpertService.GetDetailAsync(id, cancellationToken);
        var validFor = TimeSpan.FromMinutes(15);

        var cvUrl = await TrySignAsync(detail.Current.CvUrl, validFor, cancellationToken);

        var certificateUrls = new List<string>();
        var unresolvedCertificates = new List<string>();
        foreach (var certificatePath in detail.Current.Certificates)
        {
            var signed = await TrySignAsync(certificatePath, validFor, cancellationToken);
            if (signed is null)
            {
                // Panelde sessizce kaybolmasın: admin, bu satırın indirilebilir bir
                // belge OLMADIĞINI görmeli. Aksi hâlde "sertifika eksik" sanır.
                unresolvedCertificates.Add(certificatePath);
            }
            else
            {
                certificateUrls.Add(signed);
            }
        }

        return Ok(new { cvUrl, certificateUrls, unresolvedCertificates });
    }

    /// <summary>İmzalar; yol boşsa ya da Storage reddederse <c>null</c> döner.</summary>
    private async Task<string?> TrySignAsync(
        string? path,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return await storage.CreateSignedUrlAsync("documents", path, validFor, cancellationToken);
        }
        catch (DomainException)
        {
            return null;
        }
    }
}
