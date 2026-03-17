using BlazorWebAppWithKeycloak.API.Data;
using BlazorWebAppWithKeycloak.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorWebAppWithKeycloak.API.Extensions;

/// <summary>
/// Extension methods op <see cref="IEndpointRouteBuilder"/> voor de
/// todo-endpoints. Alle endpoints vereisen de <c>user</c> rol en
/// werken uitsluitend op de items van de ingelogde gebruiker.
/// </summary>
public static class TodoEndpointExtensions
{
    public static IEndpointRouteBuilder MapTodoEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/todos")
            .RequireAuthorization("UserRole")
            .WithTags("Todo");

        // GET /api/todos — alle items van de ingelogde gebruiker
        group.MapGet("/", async (HttpContext ctx, TodoDbContext db) =>
        {
            var username = GetUsername(ctx);
            var items = await db.TodoItems
                .Where(t => t.Username == username)
                .OrderBy(t => t.Afgerond)
                .ThenBy(t => t.Vervaldatum)
                .ThenByDescending(t => t.Prioriteit)
                .Select(t => TodoResponse.FromEntity(t))
                .ToListAsync();

            return Results.Ok(items);
        })
        .WithName("GetTodos")
        .WithSummary("Haalt alle todo-items op van de ingelogde gebruiker");

        // GET /api/todos/{id} — één item
        group.MapGet("/{id:int}", async (int id, HttpContext ctx, TodoDbContext db) =>
        {
            var username = GetUsername(ctx);
            var item = await db.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.Username == username);

            return item is null
                ? Results.NotFound()
                : Results.Ok(TodoResponse.FromEntity(item));
        })
        .WithName("GetTodoById")
        .WithSummary("Haalt één todo-item op van de ingelogde gebruiker");

        // POST /api/todos — nieuw item aanmaken
        group.MapPost("/", async (
            TodoAanmakenRequest request,
            HttpContext ctx,
            TodoDbContext db) =>
        {
            var username = GetUsername(ctx);
            var item = new TodoItem
            {
                Username     = username,
                Titel        = request.Titel,
                Omschrijving = request.Omschrijving,
                Prioriteit   = request.Prioriteit,
                Vervaldatum  = request.Vervaldatum,
            };

            db.TodoItems.Add(item);
            await db.SaveChangesAsync();

            return Results.Created($"/api/todos/{item.Id}", TodoResponse.FromEntity(item));
        })
        .WithName("CreateTodo")
        .WithSummary("Maakt een nieuw todo-item aan voor de ingelogde gebruiker");

        // PUT /api/todos/{id} — item bijwerken
        group.MapPut("/{id:int}", async (
            int id,
            TodoBijwerkenRequest request,
            HttpContext ctx,
            TodoDbContext db) =>
        {
            var username = GetUsername(ctx);
            var item = await db.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.Username == username);

            if (item is null)
                return Results.NotFound();

            if (request.Titel        is not null) item.Titel        = request.Titel;
            if (request.Omschrijving is not null) item.Omschrijving = request.Omschrijving;
            if (request.Afgerond     is not null) item.Afgerond     = request.Afgerond.Value;
            if (request.Prioriteit   is not null) item.Prioriteit   = request.Prioriteit.Value;
            if (request.Vervaldatum  is not null) item.Vervaldatum  = request.Vervaldatum;

            item.GewijzigdOp = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(TodoResponse.FromEntity(item));
        })
        .WithName("UpdateTodo")
        .WithSummary("Werkt een bestaand todo-item bij");

        // PATCH /api/todos/{id}/afgerond — snel als afgerond markeren
        group.MapPatch("/{id:int}/afgerond", async (
            int id,
            HttpContext ctx,
            TodoDbContext db) =>
        {
            var username = GetUsername(ctx);
            var item = await db.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.Username == username);

            if (item is null)
                return Results.NotFound();

            item.Afgerond    = !item.Afgerond;   // toggle
            item.GewijzigdOp = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(TodoResponse.FromEntity(item));
        })
        .WithName("ToggleTodoAfgerond")
        .WithSummary("Markeert een todo-item als afgerond of niet-afgerond");

        // DELETE /api/todos/{id} — item verwijderen
        group.MapDelete("/{id:int}", async (int id, HttpContext ctx, TodoDbContext db) =>
        {
            var username = GetUsername(ctx);
            var item = await db.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.Username == username);

            if (item is null)
                return Results.NotFound();

            db.TodoItems.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteTodo")
        .WithSummary("Verwijdert een todo-item van de ingelogde gebruiker");

        return endpoints;
    }

    /// <summary>
    /// Leest de <c>preferred_username</c> claim uit het JWT-token.
    /// Consistent met de NameClaimType instelling in ConfigureKeycloakOptions.
    /// </summary>
    private static string GetUsername(HttpContext ctx)
        => ctx.User.Identity?.Name
           ?? throw new InvalidOperationException("Geen gebruikersnaam gevonden in token.");
}
