using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace BlazorWebAppWithKeycloak.Auth;

/// <summary>
/// Extension methods op <see cref="IServiceCollection"/> voor de
/// Keycloak-authenticatie registratie.
/// </summary>
public static class AuthServiceExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services)
    {
        services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureKeycloakOptions>();

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
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;

                // Beperk de cookie-levensduur expliciet.
                // Standaard is dit een session cookie (verdwijnt bij sluiten browser),
                // maar een expliciete timeout beschermt tegen langlopende sessies.
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(options =>
            {
                // SameSite=Unspecified: geen SameSite-attribuut in de Set-Cookie header.
                // Nodig voor cross-site redirects van Keycloak terug naar de app (HTTP).
                options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
                options.NonceCookie.SameSite = SameSiteMode.Unspecified;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddCascadingAuthenticationState();

        return services;
    }
}
