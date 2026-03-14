using BlazorWebAppWithKeycloak.Auth;
using BlazorWebAppWithKeycloak.Components;
using BlazorWebAppWithKeycloak.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ─── Razor Components / Blazor ───────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Authenticatie & autorisatie ─────────────────────────────────────────────
builder.Services.AddKeycloakAuthentication();

// ─── Data Protection ─────────────────────────────────────────────────────────
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/keys";

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("BlazorWebAppWithKeycloak");

// ─── API Client ───────────────────────────────────────────────────────────────
// BearerTokenHandler leest het access token uit de huidige HTTP-context
// en voegt het toe als Authorization Bearer header aan elke API-aanroep.
builder.Services.AddScoped<BearerTokenHandler>();

builder.Services
    .AddHttpClient<HelloWorldApiClient>(client =>
    {
        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
            ?? "http://localhost:5001";
        client.BaseAddress = new Uri(apiBaseUrl);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");

    if (builder.Configuration["ASPNETCORE_URLS"]?.Contains("https") == true ||
        builder.Configuration["ASPNETCORE_HTTPS_PORT"] != null)
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints ────────────────────────────────────────────────────────────────
app.MapAuthEndpoints();

// ─── Blazor ───────────────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();