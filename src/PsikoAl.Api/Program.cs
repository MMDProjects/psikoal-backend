using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PsikoAl.Api.Middleware;
using PsikoAl.Services;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services
    .AddOptions<SupabaseOptions>()
    .Bind(builder.Configuration.GetSection(SupabaseOptions.SectionName))
    .ValidateOnStart();

var supabase = builder.Configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>();

builder.Services.AddHttpClient<ISupabaseAuthService, SupabaseAuthService>(client =>
{
    if (supabase is null)
    {
        return;
    }

    client.BaseAddress = new Uri(supabase.Url.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("apikey", supabase.AnonKey);
});

// Supabase artık JWT'leri asimetrik anahtarlarla (ES256) imzalıyor ve JWKS/OIDC discovery yayınlıyor;
// paylaşılan "JWT Secret" (legacy HS256) yerine standart Authority tabanlı doğrulama kullanılır.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (supabase is not null)
        {
            options.Authority = supabase.Url.TrimEnd('/') + "/auth/v1";
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = ["authenticated"],
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<DomainExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
