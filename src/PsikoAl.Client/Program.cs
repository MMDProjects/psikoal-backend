using PsikoAl.Client.Components;
using PsikoAl.Client.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<AdminSessionState>();
builder.Services.AddHttpClient<IAdminClientService, AdminClientService>(client =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(apiBaseUrl))
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
