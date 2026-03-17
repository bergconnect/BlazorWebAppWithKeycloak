using BlazorWebAppWithKeycloak.API.Data;
using BlazorWebAppWithKeycloak.API.Extensions;
using BlazorWebAppWithKeycloak.API.Repositories;
using BlazorWebAppWithKeycloak.API.Services;
using Keycloak.Auth.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Keycloak JWT authenticatie ───────────────────────────────────────────────
builder.Services.AddKeycloakApiAuth(options =>
{
    options.AddPolicy("AdminRole", policy => policy.RequireRole("admin"));
});

// ─── EF Core — SQLite ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoDb")
        ?? "Data Source=todo.db"));

// ─── Repository & Service ─────────────────────────────────────────────────────
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ITodoService, TodoService>();

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
