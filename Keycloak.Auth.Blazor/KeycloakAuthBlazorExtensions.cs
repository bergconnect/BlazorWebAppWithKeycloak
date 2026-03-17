using Keycloak.Auth.Blazor.Internal;
using Keycloak.Auth.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Keycloak.Auth.Blazor;

/// <summary>
/// Extensiemethoden voor het registreren van Keycloak-authenticatie
/// in een Blazor Server applicatie.
///
/// Gebruik:
/// <code>
/// // Program.cs — services
/// builder.Services.AddKeycloakBlazorAuth(builder.Environment);
///
/// // Program.cs — middleware (na UseAuthentication/UseAuthorization)
/// app.MapKeycloakAuthEndpoints();
/// </code>
/// </summary>
public static class KeycloakAuthBlazorExtensions
{
    /// <summary>
    /// Registreert alle Keycloak OIDC-authenticatie services inclusief
    /// cookie-instellingen, token services en de BearerTokenHandler.
    ///
    /// Leest configuratie uit de sectie "Keycloak" in appsettings.json.
    /// </summary>
    public static IServiceCollection AddKeycloakBlazorAuth(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        // Op HTTPS (productie achter reverse proxy) de Secure-flag instellen.
        // Op HTTP (lokale development) is None vereist — browsers weigeren
        // cookies met Secure-flag over HTTP, waardoor de OIDC-correlatie mislukt.
        var securePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;

        var sameSite = environment.IsDevelopment()
            ? SameSiteMode.Unspecified
            : SameSiteMode.Lax;

        // ── Configuratie ──────────────────────────────────────────────────────
        services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureKeycloakOptions>();

        // ── Authenticatie ─────────────────────────────────────────────────────
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = securePolicy;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(options =>
            {
                options.CorrelationCookie.SameSite = sameSite;
                options.CorrelationCookie.SecurePolicy = securePolicy;
                options.NonceCookie.SameSite = sameSite;
                options.NonceCookie.SecurePolicy = securePolicy;
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddCascadingAuthenticationState();

        // ── Token services ────────────────────────────────────────────────────
        services.AddScoped<TokenProvider>();
        services.AddScoped<TokenService>();
        services.AddScoped<BearerTokenHandler>();

        return services;
    }

    /// <summary>
    /// Registreert de <c>/login</c> en <c>/logout</c> endpoints.
    /// Roep aan na <c>app.UseAuthentication()</c> en <c>app.UseAuthorization()</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapKeycloakAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
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
                new AuthenticationProperties
                {
                    RedirectUri = redirectUri
                });
        })
        .AllowAnonymous()
        .DisableAntiforgery();
    }

    private static void MapLogoutEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        })
        .RequireAuthorization()
        .DisableAntiforgery();
    }

    private static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\");
    }
}
