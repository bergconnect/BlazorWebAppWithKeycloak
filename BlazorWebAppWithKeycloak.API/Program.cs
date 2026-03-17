using BlazorWebAppWithKeycloak.API.Auth;
using BlazorWebAppWithKeycloak.API.Data;
using BlazorWebAppWithKeycloak.API.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Authenticatie & autorisatie ─────────────────────────────────────────────
builder.Services.AddKeycloakJwtAuthentication();

// ─── EF Core — SQLite ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoDb")
        ?? "Data Source=todo.db"));

// ─── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ─── Database aanmaken bij opstarten ─────────────────────────────────────────
// EnsureCreatedAsync maakt de database en alle tabellen aan vanuit het model
// als ze nog niet bestaan. Geen migratiebestanden nodig.
// Let op: bij schemawijzigingen de database verwijderen en opnieuw opstarten,
// of overstappen op migraties voor behoud van bestaande data.
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
