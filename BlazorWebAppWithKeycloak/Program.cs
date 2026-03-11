using BlazorWebAppWithKeycloak.Components;
using BlazorWebAppWithKeycloak.Extentions;

var builder = WebApplication.CreateBuilder(args);

// ─── Razor Components / Blazor ───────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Authenticatie & autorisatie ─────────────────────────────────────────────
builder.Services.AddKeycloakAuthentication();

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
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