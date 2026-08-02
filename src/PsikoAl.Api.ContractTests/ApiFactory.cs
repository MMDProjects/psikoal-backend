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
            });
        });

        return base.CreateHost(builder);
    }
}
