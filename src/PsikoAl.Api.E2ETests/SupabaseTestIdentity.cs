using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace PsikoAl.Api.E2ETests;

/// <summary>Lokal GoTrue üzerinde gerçek kullanıcı üretir ve gerçek JWT alır.</summary>
/// <remarks>
/// Token sahte imzalanmaz. Amaç tam olarak Supabase'in ürettiği token'ın API'nin
/// JwtBearer doğrulamasından geçmesini kanıtlamak; sahte imza bu zinciri atlar ve
/// prod'da patlayan bir konfigürasyonu testte yeşil gösterir.
/// </remarks>
internal static class SupabaseTestIdentity
{
    // Sabit e-posta bilinçli: her koşumda yeni kullanıcı üretmek auth.users'ı şişirir
    // ve GoTrue'nun kayıt rate limit'ine takılır. Kullanıcı bir kez üretilir, sonra bulunur.
    //
    // DİKKAT — bu dört sabit üç yerde daha görünür ve BİREBİR aynı olmak zorunda:
    //   scripts/e2e-stack.sh          (Playwright ve Maestro'yu besleyen ortam)
    //   .github/workflows/e2e.yml     (CI ortamı)
    //   e2e/mobile/flows/*.yaml       (Maestro akışlarının login adımı)
    // Ayrışırlarsa belirti "401" olur ve sözleşme hatası sanılır.
    public const string AdminEmail = "e2e-admin@psikoal.test";
    public const string AdminPassword = "E2eAdminPass123!";

    public const string ClientEmail = "e2e-client@psikoal.test";
    public const string ClientPassword = "E2eClientPass123!";

    public static async Task<AuthenticatedUser> EnsureAdminAsync(CancellationToken cancellationToken)
    {
        var authUserId = await EnsureAuthUserAsync(AdminEmail, AdminPassword, cancellationToken);
        await EnsureAdminRowAsync(authUserId, cancellationToken);
        var accessToken = await SignInAsync(AdminEmail, AdminPassword, cancellationToken);
        return new AuthenticatedUser(authUserId, AdminEmail, accessToken);
    }

    /// <summary>
    /// Mobil akışların giriş yaptığı danışan hesabını garanti eder.
    /// </summary>
    /// <remarks>
    /// admin_users satırı EKLENMEZ — bu hesabın admin uçlarına erişememesi bir
    /// özellik: yetki sınırının gerçekten çalıştığını mobil tarafta da doğrular.
    /// </remarks>
    public static async Task<AuthenticatedUser> EnsureClientAsync(CancellationToken cancellationToken)
    {
        var authUserId = await EnsureAuthUserAsync(ClientEmail, ClientPassword, cancellationToken);
        var accessToken = await SignInAsync(ClientEmail, ClientPassword, cancellationToken);
        return new AuthenticatedUser(authUserId, ClientEmail, accessToken);
    }

    private static async Task<Guid> EnsureAuthUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient(LocalStack.ServiceRoleKey);

        var response = await client.PostAsJsonAsync(
            "auth/v1/admin/users",
            new { email, password, email_confirm = true },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return created.GetProperty("id").GetGuid();
        }

        // Kullanıcı zaten varsa GoTrue 422 döner. Bu bir hata değil, ikinci koşumun
        // normal hâli — mevcut kullanıcıyı bul ve şifresini bilinen değere sabitle.
        var existing = await FindAuthUserIdAsync(client, email, cancellationToken)
            ?? throw new InvalidOperationException(
                $"GoTrue kullanıcı üretemedi ve mevcut kullanıcı da bulunamadı. "
                + $"Durum: {(int)response.StatusCode}, gövde: "
                + await response.Content.ReadAsStringAsync(cancellationToken));

        var reset = await client.PutAsJsonAsync(
            $"auth/v1/admin/users/{existing}",
            new { password, email_confirm = true },
            cancellationToken);
        reset.EnsureSuccessStatusCode();

        return existing;
    }

    private static async Task<Guid?> FindAuthUserIdAsync(
        HttpClient client,
        string email,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(
            $"auth/v1/admin/users?filter={Uri.EscapeDataString(email)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        if (!body.TryGetProperty("users", out var users))
        {
            return null;
        }

        foreach (var user in users.EnumerateArray())
        {
            if (string.Equals(
                    user.GetProperty("email").GetString(),
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return user.GetProperty("id").GetGuid();
            }
        }

        return null;
    }

    private static async Task EnsureAdminRowAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        // admin_users'a RLS açık ve policy YOK — yalnız service_role erişir. Test
        // doğrudan Postgres'e bağlanıyor (postgres superuser), RLS baypası burada
        // kasıtlı ve testin kendi kurulum adımına ait.
        await using var dataSource = new NpgsqlDataSourceBuilder(LocalStack.PostgresConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            insert into public.admin_users (auth_user_id, display_name, role, is_active)
            values (@auth_user_id, 'E2E Admin', 'super_admin', true)
            on conflict (auth_user_id) do update set is_active = true;
            """;
        command.Parameters.AddWithValue("auth_user_id", authUserId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient(LocalStack.AnonKey);

        var response = await client.PostAsJsonAsync(
            "auth/v1/token?grant_type=password",
            new { email, password },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"GoTrue login başarısız ({(int)response.StatusCode}): "
                + await response.Content.ReadAsStringAsync(cancellationToken));
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token boş döndü.");
    }

    private static HttpClient CreateClient(string apiKey)
    {
        var client = new HttpClient { BaseAddress = new Uri(LocalStack.SupabaseUrl.TrimEnd('/') + "/") };
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }
}

internal sealed record AuthenticatedUser(Guid AuthUserId, string Email, string AccessToken);
