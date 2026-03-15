using BlazorWebAppWithKeycloak.API.Auth;
using BlazorWebAppWithKeycloak.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ─── Authenticatie & autorisatie ─────────────────────────────────────────────
builder.Services.AddKeycloakJwtAuthentication();

// ─── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints ────────────────────────────────────────────────────────────────
app.MapHelloEndpoints();

app.Run();
