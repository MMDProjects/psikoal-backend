using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PsikoAl.Api.Authorization;
using PsikoAl.Api.Middleware;
using PsikoAl.Api.Serialization;
using PsikoAl.Data;
using PsikoAl.Data.Repositories;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Options;

var builder = WebApplication.CreateBuilder(args);

// DİKKAT — öncelik sırası: bu satır CreateBuilder'dan SONRA geldiği için
// appsettings.Local.json, user-secrets'ı EZER. Normal geliştirme akışı user-secrets'tır
// (bkz. appsettings.Local.example.json); bu dosya yalnız container/mount senaryosu için
// bir kaçış kapısı olarak duruyor. Düz metin prod anahtarı ASLA buraya yazılmaz.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services
    .AddOptions<SupabaseOptions>()
    .Bind(builder.Configuration.GetSection(SupabaseOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<BrevoOptions>()
    .Bind(builder.Configuration.GetSection(BrevoOptions.SectionName));

var supabase = builder.Configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    }
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddHttpClient<ISupabaseAuthService, SupabaseAuthService>(client =>
{
    if (supabase is null)
    {
        return;
    }

    client.BaseAddress = new Uri(supabase.Url.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("apikey", supabase.AnonKey);
});

builder.Services.AddHttpClient<ISupabaseAdminService, SupabaseAdminService>(client =>
{
    if (supabase is null)
    {
        return;
    }

    client.BaseAddress = new Uri(supabase.Url.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("apikey", supabase.ServiceRoleKey);
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + supabase.ServiceRoleKey);
});

builder.Services.AddHttpClient<ISupabaseStorageService, SupabaseStorageService>(client =>
{
    if (supabase is null)
    {
        return;
    }

    client.BaseAddress = new Uri(supabase.Url.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("apikey", supabase.ServiceRoleKey);
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + supabase.ServiceRoleKey);
});

builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/");
});

builder.Services.AddHttpClient<IPushNotificationService, ExpoPushNotificationService>(client =>
{
    client.BaseAddress = new Uri("https://exp.host/");
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IExpertService, ExpertService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminExpertService, AdminExpertService>();
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
builder.Services.AddScoped<IAdminReviewService, AdminReviewService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IAdminListingService, AdminListingService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IAdminMatchService, AdminMatchService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPushTokenService, PushTokenService>();
builder.Services.AddScoped<IAdminNotificationTemplateService, AdminNotificationTemplateService>();
builder.Services.AddScoped<IAdminNotificationService, AdminNotificationService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IAdminAssessmentService, AdminAssessmentService>();
builder.Services.AddScoped<IAdminGuard, AdminGuard>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AdminRequirementHandler>();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new AdminRequirement()));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
        }));
    options.AddPolicy("upload", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
        }));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter()));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<DomainExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
