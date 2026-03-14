using BlazorWebAppWithKeycloak.Auth;
using BlazorWebAppWithKeycloak.Components;

var builder = WebApplication.CreateBuilder(args);

// ─── Razor Components / Blazor ───────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Authenticatie & autorisatie ─────────────────────────────────────────────
builder.Services.AddKeycloakAuthentication();

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");

    // Alleen HSTS en HTTPS-redirect inschakelen als de app daadwerkelijk
    // via HTTPS bereikbaar is. Achter een HTTP-only reverse proxy of bij
    // directe HTTP-toegang leidt dit anders tot redirect-loops en
    // "Failed to determine the https port" warnings.
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