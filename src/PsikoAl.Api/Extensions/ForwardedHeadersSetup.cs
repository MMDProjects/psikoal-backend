using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace PsikoAl.Api.Extensions;

// API bir ters vekilin (load balancer / reverse proxy) arkasında koşuyor. X-Forwarded-For
// okunmazsa RemoteIpAddress her istekte vekilin adresi olur ve IP başına rate limit
// TÜM kullanıcıları tek kovaya koyar. Yalnız yapılandırmada adı geçen vekillere güvenilir
// (varsayılan: loopback) — aksi hâlde istemci başlığı sahteleyip limiti atlatırdı.
public static class ForwardedHeadersSetup
{
    public const string SectionName = "ForwardedHeaders";

    public static IServiceCollection AddConfiguredForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var knownProxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var knownNetworks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];
        var forwardLimit = section.GetValue("ForwardLimit", 1);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = forwardLimit;

            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }
}
