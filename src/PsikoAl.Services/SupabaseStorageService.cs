using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class SupabaseStorageService(HttpClient httpClient) : ISupabaseStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> UploadAsync(
        string bucket,
        string path,
        Stream content,
        string contentType,
        bool isPublicBucket,
        CancellationToken cancellationToken)
    {
        using var body = new StreamContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{bucket}/{path}")
        {
            Content = body,
        };
        request.Headers.Add("x-upsert", "true");

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new DomainException(ErrorKeys.StorageUploadFailed);
        }

        return isPublicBucket
            ? $"{httpClient.BaseAddress}storage/v1/object/public/{bucket}/{path}"
            : path;
    }

    public async Task<string> CreateSignedUrlAsync(
        string bucket,
        string path,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"storage/v1/object/sign/{bucket}/{path}",
            new { expiresIn = (int)validFor.TotalSeconds },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new DomainException(ErrorKeys.StorageUploadFailed);
        }

        var signed = await response.Content.ReadFromJsonAsync<SignedUrlResponse>(JsonOptions, cancellationToken)
            ?? throw new DomainException(ErrorKeys.StorageUploadFailed);
        return $"{httpClient.BaseAddress}storage/v1{signed.SignedUrl.TrimStart('/')}";
    }

    private sealed record SignedUrlResponse([property: JsonPropertyName("signedURL")] string SignedUrl);
}
