using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Common.Validation;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class UploadService(
    IUnitOfWork unitOfWork,
    ISupabaseStorageService storage) : IUploadService
{
    private static readonly string[] ImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private static readonly string[] DocumentContentTypes = ["application/pdf", "image/jpeg", "image/png"];

    public async Task<string> UploadAvatarAsync(
        Guid userId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var profile = await unitOfWork.Profiles.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        var validated = await ValidateAsync(content, contentType, ImageContentTypes, cancellationToken);
        var path = $"{userId}/avatar.{FileSignatures.ExtensionFor(contentType)}";
        var url = await storage.UploadAsync("avatars", path, validated, contentType, isPublicBucket: true, cancellationToken);

        profile.AvatarUrl = url;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return url;
    }

    public async Task<string> UploadCvAsync(
        Guid userId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var expert = await unitOfWork.Experts.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ExpertNotFound);

        var validated = await ValidateAsync(content, contentType, DocumentContentTypes, cancellationToken);
        var path = $"{userId}/cv.{FileSignatures.ExtensionFor(contentType)}";
        var storedPath = await storage.UploadAsync("documents", path, validated, contentType, isPublicBucket: false, cancellationToken);

        expert.CvUrl = storedPath;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return storedPath;
    }

    public async Task<IReadOnlyList<string>> UploadCertificateAsync(
        Guid userId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var expert = await unitOfWork.Experts.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ExpertNotFound);

        var validated = await ValidateAsync(content, contentType, DocumentContentTypes, cancellationToken);
        var path = $"{userId}/certificates/{Guid.NewGuid()}.{FileSignatures.ExtensionFor(contentType)}";
        var storedPath = await storage.UploadAsync("documents", path, validated, contentType, isPublicBucket: false, cancellationToken);

        expert.Certificates = [.. expert.Certificates, storedPath];
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return expert.Certificates;
    }

    private static async Task<Stream> ValidateAsync(
        Stream content,
        string contentType,
        string[] allowedContentTypes,
        CancellationToken cancellationToken)
    {
        if (!allowedContentTypes.Contains(contentType) || !FileSignatures.IsSupported(contentType))
        {
            throw new DomainException(ErrorKeys.FileTypeNotAllowed, "file");
        }

        // Content-Type header'ına güvenilmez: gerçek dosya imzası (magic bytes) doğrulanır.
        var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        if (!FileSignatures.Matches(contentType, buffered.GetBuffer().AsSpan(0, (int)Math.Min(buffered.Length, 8))))
        {
            throw new DomainException(ErrorKeys.FileTypeNotAllowed, "file");
        }

        buffered.Position = 0;
        return buffered;
    }
}
