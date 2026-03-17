using BlazorWebAppWithKeycloak.API.Data;
using BlazorWebAppWithKeycloak.API.Extensions;
using Keycloak.Auth.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Keycloak JWT authenticatie ───────────────────────────────────────────────
// Registreert JWT Bearer-validatie en de UserRole policy.
// Leest configuratie uit de sectie "Keycloak" in appsettings.json.
// Optioneel: geef extra policies mee via de lambda.
builder.Services.AddKeycloakApiAuth(options =>
{
    options.AddPolicy("AdminRole", policy => policy.RequireRole("admin"));
});

// ─── EF Core — SQLite ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoDb")
        ?? "Data Source=todo.db"));

// ─── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ─── Database aanmaken bij opstarten ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints ────────────────────────────────────────────────────────────────
app.MapTodoEndpoints();

app.Run();