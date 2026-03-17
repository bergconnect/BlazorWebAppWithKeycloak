using BlazorWebAppWithKeycloak.API.Models;
using BlazorWebAppWithKeycloak.API.Services;

namespace BlazorWebAppWithKeycloak.API.Extensions;

/// <summary>
/// Extension methods op <see cref="IEndpointRouteBuilder"/> voor de
/// todo-endpoints. Verantwoordelijk voor routing en HTTP-vertaling.
/// Businesslogica en mapping verlopen via <see cref="ITodoService"/>.
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

        // GET /api/todos
        group.MapGet("/", async (HttpContext ctx, ITodoService service, CancellationToken ct) =>
        {
            var items = await service.GetAlleAsync(GetUsername(ctx), ct);
            return Results.Ok(items);
        })
        .WithName("GetTodos")
        .WithSummary("Haalt alle todo-items op van de ingelogde gebruiker");

        // GET /api/todos/{id}
        group.MapGet("/{id:int}", async (int id, HttpContext ctx, ITodoService service, CancellationToken ct) =>
        {
            var item = await service.GetAsync(id, GetUsername(ctx), ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetTodoById")
        .WithSummary("Haalt één todo-item op van de ingelogde gebruiker");

        // POST /api/todos
        group.MapPost("/", async (
            TodoAanmakenRequest request,
            HttpContext ctx,
            ITodoService service,
            CancellationToken ct) =>
        {
            var aangemaakt = await service.AanmakenAsync(request, GetUsername(ctx), ct);
            return Results.Created($"/api/todos/{aangemaakt.Id}", aangemaakt);
        })
        .WithName("CreateTodo")
        .WithSummary("Maakt een nieuw todo-item aan voor de ingelogde gebruiker");

        // PUT /api/todos/{id}
        group.MapPut("/{id:int}", async (
            int id,
            TodoBijwerkenRequest request,
            HttpContext ctx,
            ITodoService service,
            CancellationToken ct) =>
        {
            var bijgewerkt = await service.BijwerkenAsync(id, request, GetUsername(ctx), ct);
            return bijgewerkt is null ? Results.NotFound() : Results.Ok(bijgewerkt);
        })
        .WithName("UpdateTodo")
        .WithSummary("Werkt een bestaand todo-item bij");

        // PATCH /api/todos/{id}/afgerond
        group.MapPatch("/{id:int}/afgerond", async (
            int id,
            HttpContext ctx,
            ITodoService service,
            CancellationToken ct) =>
        {
            var bijgewerkt = await service.ToggleAfgerondAsync(id, GetUsername(ctx), ct);
            return bijgewerkt is null ? Results.NotFound() : Results.Ok(bijgewerkt);
        })
        .WithName("ToggleTodoAfgerond")
        .WithSummary("Markeert een todo-item als afgerond of niet-afgerond");

        // DELETE /api/todos/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            HttpContext ctx,
            ITodoService service,
            CancellationToken ct) =>
        {
            var verwijderd = await service.VerwijderenAsync(id, GetUsername(ctx), ct);
            return verwijderd ? Results.NoContent() : Results.NotFound();
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
