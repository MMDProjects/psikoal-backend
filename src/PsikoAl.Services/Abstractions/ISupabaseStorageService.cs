namespace PsikoAl.Services.Abstractions;

public interface ISupabaseStorageService
{
    /// Public bucket'ta kalıcı public URL, private bucket'ta obje path'i döner.
    Task<string> UploadAsync(
        string bucket,
        string path,
        Stream content,
        string contentType,
        bool isPublicBucket,
        CancellationToken cancellationToken);

    Task<string> CreateSignedUrlAsync(
        string bucket,
        string path,
        TimeSpan validFor,
        CancellationToken cancellationToken);
}
