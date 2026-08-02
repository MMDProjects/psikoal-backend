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
                ["Supabase:Url"] = "http://localhost:54321",
                ["Supabase:AnonKey"] = "test-anon-key",
                ["Supabase:ServiceRoleKey"] = "test-service-role-key",
                ["Supabase:JwtSecret"] = "test-jwt-secret-with-at-least-32-chars!",
            });
        });

        return base.CreateHost(builder);
    }
}
