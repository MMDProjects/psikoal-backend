using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Support;

public sealed class FakeStorageService : ISupabaseStorageService
{
    public List<(string Bucket, string Folder)> DeletedFolders { get; } = [];

    public List<(string Bucket, string Path)> DeletedObjects { get; } = [];

    public List<(string Bucket, string Path)> SignedPaths { get; } = [];

    public Exception? DeleteFailure { get; set; }

    public Task<string> UploadAsync(
        string bucket,
        string path,
        Stream content,
        string contentType,
        bool isPublicBucket,
        CancellationToken cancellationToken)
        => Task.FromResult(path);

    public Task DeleteFolderAsync(string bucket, string folder, CancellationToken cancellationToken)
    {
        if (DeleteFailure is not null)
        {
            throw DeleteFailure;
        }

        DeletedFolders.Add((bucket, folder));
        return Task.CompletedTask;
    }

    public Task DeleteObjectAsync(string bucket, string path, CancellationToken cancellationToken)
    {
        if (DeleteFailure is not null)
        {
            throw DeleteFailure;
        }

        DeletedObjects.Add((bucket, path));
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string bucket, string path) => $"https://public.test/{bucket}/{path}";

    public Task<string> CreateSignedUrlAsync(
        string bucket,
        string path,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        SignedPaths.Add((bucket, path));
        return Task.FromResult($"https://signed.test/{bucket}/{path}");
    }
}
