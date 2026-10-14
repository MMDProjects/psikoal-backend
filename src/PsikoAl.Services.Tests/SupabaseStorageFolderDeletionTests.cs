using System.Net;
using System.Text;
using PsikoAl.Services;

namespace PsikoAl.Services.Tests;

// Hesap silmede documents/{userId}/ altındaki CV ve certificates/ alt klasörü de gitmeli.
// Storage LIST tek seviye döner; klasör id'siz satırdır ve ayrıca gezilmezse
// sertifikalar sessizce kalırdı.
public sealed class SupabaseStorageFolderDeletionTests
{
    [Fact]
    public async Task DeleteFolderAsync_walks_subfolders_and_deletes_every_object()
    {
        var handler = new RoutingHandler(new Dictionary<string, string>
        {
            ["u1"] = """[{"name":"cv.pdf","id":"a"},{"name":"certificates","id":null}]""",
            ["u1/certificates"] = """[{"name":"c1.pdf","id":"b"}]""",
        });
        var service = new SupabaseStorageService(new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:54321/") });

        await service.DeleteFolderAsync("documents", "u1", CancellationToken.None);

        var delete = Assert.Single(handler.DeleteBodies);
        Assert.Contains("\"u1/cv.pdf\"", delete, StringComparison.Ordinal);
        Assert.Contains("\"u1/certificates/c1.pdf\"", delete, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteFolderAsync_on_an_empty_folder_sends_no_delete()
    {
        var handler = new RoutingHandler(new Dictionary<string, string> { ["u2"] = "[]" });
        var service = new SupabaseStorageService(new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:54321/") });

        await service.DeleteFolderAsync("avatars", "u2", CancellationToken.None);

        Assert.Empty(handler.DeleteBodies);
    }

    private sealed class RoutingHandler(Dictionary<string, string> listings) : HttpMessageHandler
    {
        public List<string> DeleteBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            if (request.Method == HttpMethod.Delete)
            {
                DeleteBodies.Add(body);
                return Json("[]");
            }

            var prefix = listings.Keys.Single(key => body.Contains($"\"prefix\":\"{key}\"", StringComparison.Ordinal));
            return Json(listings[prefix]);
        }

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
