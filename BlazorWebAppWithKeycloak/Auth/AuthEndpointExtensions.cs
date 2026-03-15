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
            var redirectUri = IsLocalUrl(returnUrl) ? returnUrl! : "/";

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
            // Verwijder zowel de applicatiecookie als de OIDC-sessie.
            // SignOut op beide schemes zorgt dat Keycloak ook de SSO-sessie
            // beëindigt en de gebruiker niet automatisch opnieuw inlogt.
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        })
        .RequireAuthorization()
        .DisableAntiforgery();
    }

    /// <summary>
    /// Controleert of de opgegeven URL een lokale (relatieve) URL is
    /// om open-redirect kwetsbaarheden te voorkomen.
    /// </summary>
    private static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        // Moet beginnen met "/" maar niet met "//" (protocol-relative redirect)
        // en niet met "/\" (Windows path separator trick)
        return url.StartsWith('/') 
            && !url.StartsWith("//") 
            && !url.StartsWith("/\\");
    }
}
