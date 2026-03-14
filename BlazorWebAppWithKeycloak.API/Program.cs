using BlazorWebAppWithKeycloak.API.Auth;

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

// GET /api/hello — alleen toegankelijk voor gebruikers met de "user" rol
app.MapGet("/api/hello", (HttpContext ctx) =>
{
    var username = ctx.User.Identity?.Name ?? "onbekend";
    return Results.Ok(new
    {
        Message = $"Hallo, {username}!",
        Timestamp = DateTimeOffset.UtcNow
    });
})
.RequireAuthorization("UserRole")
.WithName("HelloWorld")
.WithSummary("Hello World endpoint — vereist de 'user' rol")
.WithTags("Hello");

app.Run();