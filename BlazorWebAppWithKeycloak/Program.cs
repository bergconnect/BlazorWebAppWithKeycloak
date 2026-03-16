using BlazorWebAppWithKeycloak.Auth;
using BlazorWebAppWithKeycloak.Components;
using BlazorWebAppWithKeycloak.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ─── Razor Components / Blazor ───────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Authenticatie & autorisatie ─────────────────────────────────────────────
builder.Services.AddKeycloakAuthentication(builder.Environment);

// ─── Data Protection ─────────────────────────────────────────────────────────
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/keys";

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("BlazorWebAppWithKeycloak");

// ─── API Client ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<TokenRefreshService>();
builder.Services.AddScoped<BearerTokenHandler>();

builder.Services
    .AddHttpClient<HelloWorldApiClient>(client =>
    {
        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
            ?? "http://localhost:5001";
        client.BaseAddress = new Uri(apiBaseUrl);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();

// ─── Forwarded Headers (alleen productie) ────────────────────────────────────
// Verwerk X-Forwarded-Proto van de reverse proxy zodat ASP.NET Core
// https://<app-domein> als basis-URL gebruikt voor redirect URIs.
// Lokaal (Development) is er geen proxy en worden deze headers niet ingeschakeld.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Vertrouw alle proxies in het interne netwerk.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    // Forwarded headers EERST verwerken zodat alle volgende middleware
    // de correcte protocol- en host-informatie ziet.
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Error");
}
else
{
    app.UseDeveloperExceptionPage();
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
