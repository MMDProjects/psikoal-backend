using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PsikoAl.Api.ContractTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // https şart: Authority set edildiğinde JwtBearer'ın RequireHttpsMetadata
                // kontrolü http şemasında TÜM istekleri (health dahil) 500'e düşürür.
                ["Supabase:Url"] = "https://localhost.supabase.test",
                ["Supabase:AnonKey"] = "test-anon-key",
                ["Supabase:ServiceRoleKey"] = "test-service-role-key",
            });
        });

        return base.CreateHost(builder);
    }
}
