namespace BlazorWebAppWithKeycloak.API.Extensions;

/// <summary>
/// Extension methods op <see cref="IEndpointRouteBuilder"/> voor de
/// Hello World-endpoints.
/// </summary>
public static class HelloEndpointExtensions
{
    /// <summary>
    /// Registreert de Hello World-endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapHelloEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/hello — toegankelijk voor gebruikers met de 'user' rol
        endpoints.MapGet("/api/hello", (HttpContext ctx) =>
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

        // GET /api/admin — toegankelijk voor gebruikers met de 'admin' rol
        endpoints.MapGet("/api/admin", (HttpContext ctx) =>
        {
            var username = ctx.User.Identity?.Name ?? "onbekend";
            return Results.Ok(new
            {
                Message = $"Welkom in het beheerdersgedeelte, {username}!",
                Timestamp = DateTimeOffset.UtcNow
            });
        })
        .RequireAuthorization("AdminRole")
        .WithName("AdminHello")
        .WithSummary("Admin endpoint — vereist de 'admin' rol")
        .WithTags("Admin");

        return endpoints;
    }
}
