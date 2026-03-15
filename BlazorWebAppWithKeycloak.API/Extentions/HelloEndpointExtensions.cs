namespace BlazorWebAppWithKeycloak.API.Extensions;

/// <summary>
/// Extension methods op <see cref="IEndpointRouteBuilder"/> voor de
/// Hello World-endpoints.
/// </summary>
public static class HelloEndpointExtensions
{
    /// <summary>
    /// Registreert GET /api/hello als minimaal API-endpoint.
    /// Alleen toegankelijk voor gebruikers met de 'user' rol.
    /// </summary>
    public static IEndpointRouteBuilder MapHelloEndpoints(this IEndpointRouteBuilder endpoints)
    {
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

        return endpoints;
    }
}
