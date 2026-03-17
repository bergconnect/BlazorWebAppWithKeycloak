using BlazorWebAppWithKeycloak.Components;
using BlazorWebAppWithKeycloak.Services;
using Keycloak.Auth.Blazor;
using Keycloak.Auth.Blazor.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ─── Razor Components / Blazor ───────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Keycloak authenticatie ───────────────────────────────────────────────────
// Registreert OIDC, cookie, TokenProvider, TokenService en BearerTokenHandler.
// Leest configuratie uit de sectie "Keycloak" in appsettings.json.
builder.Services.AddKeycloakBlazorAuth(builder.Environment);

// ─── Data Protection ─────────────────────────────────────────────────────────
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/keys";

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("BlazorWebAppWithKeycloak");

// ─── API Client ───────────────────────────────────────────────────────────────
builder.Services
    .AddHttpClient<TodoApiClient>(client =>
    {
        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
            ?? "http://localhost:5001";
        client.BaseAddress = new Uri(apiBaseUrl);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();

// ─── Forwarded Headers (alleen productie) ────────────────────────────────────
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
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
// Registreert /login en /logout via de Keycloak library
app.MapKeycloakAuthEndpoints();

// ─── Blazor ───────────────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();