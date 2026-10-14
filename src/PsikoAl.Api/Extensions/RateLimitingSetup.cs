using System.Security.Claims;
using System.Threading.RateLimiting;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos;

namespace PsikoAl.Api.Extensions;

public static class RateLimitPolicies
{
    // Kimlik bilgisi deneyen uçlar (login, register, forgot/reset/change-password): IP başına sıkı.
    public const string Auth = "auth";

    // Token yenileme her istemcide sık ve kendiliğinden çağrılıyor; "auth" limitine
    // konsaydı aynı NAT arkasındaki kullanıcılar birbirini oturumdan atardı.
    public const string Refresh = "refresh";

    public const string Upload = "upload";

    // Test gönderimi anonim ve her çağrı DB'ye satır yazıyor.
    public const string AssessmentSubmit = "assessment-submit";
}

public static class RateLimitingSetup
{
    public static IServiceCollection AddConfiguredRateLimiting(this IServiceCollection services)
        => services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ApiErrorDto(ErrorKeys.AuthRateLimited, ErrorMessagesTr.For(ErrorKeys.AuthRateLimited)),
                    cancellationToken);

            options.AddPolicy(RateLimitPolicies.Auth, context => FixedWindow(IpPartition(context), permitLimit: 10));
            options.AddPolicy(RateLimitPolicies.Refresh, context => FixedWindow(IpPartition(context), permitLimit: 120));
            options.AddPolicy(RateLimitPolicies.Upload, context => FixedWindow(UserOrIpPartition(context), permitLimit: 20));
            options.AddPolicy(RateLimitPolicies.AssessmentSubmit, context => FixedWindow(UserOrIpPartition(context), permitLimit: 30));
        });

    private static RateLimitPartition<string> FixedWindow(string partitionKey, int permitLimit)
        => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
            });

    private static string IpPartition(HttpContext context)
        => "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    // Kullanıcı kimliği, limiter kimlik doğrulamadan SONRA koştuğu için doludur.
    private static string UserOrIpPartition(HttpContext context)
        => context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId
            ? "user:" + userId
            : IpPartition(context);
}
