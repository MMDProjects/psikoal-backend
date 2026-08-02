using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PsikoAl.Api.Middleware;
using PsikoAl.Services;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Options;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = supabase is null ? null : supabase.Url.TrimEnd('/') + "/auth/v1",
            ValidateAudience = true,
            ValidAudiences = ["authenticated"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = supabase is null
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabase.JwtSecret)),
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
