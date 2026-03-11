using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace BlazorWebAppWithKeycloak.Auth;

/// <summary>
/// Extension methods op <see cref="IEndpointRouteBuilder"/> voor de
/// Keycloak login- en logout-endpoints.
/// </summary>
public static class AuthEndpointExtensions
{
    /// <summary>
    /// Registreert GET /login en GET /logout als minimale API-endpoints.
    /// Moet aangeroepen worden vóór MapRazorComponents zodat Blazor
    /// deze routes niet onderschept.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapLoginEndpoint();
        endpoints.MapLogoutEndpoint();
        return endpoints;
    }

    private static void MapLoginEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", async (HttpContext ctx, string? returnUrl) =>
        {
            var redirectUri = returnUrl ?? "/";
            await ctx.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = redirectUri });
        })
        .AllowAnonymous()
        .DisableAntiforgery();
    }

    private static void MapLogoutEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/logout", async (HttpContext ctx) =>
        {
            // Alleen de lokale applicatiesessie (cookie) beëindigen.
            // Het OIDC-scheme wordt bewust NIET uitgetekend zodat de
            // Keycloak SSO-sessie actief blijft voor andere applicaties.
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ctx.Response.Redirect("/");
        })
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}