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
    /// <summary>
    /// Registreert Keycloak OIDC-authenticatie inclusief de Options-binding,
    /// autorisatie, HttpContextAccessor en CascadingAuthenticationState.
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services)
    {
        // Options pattern: bind "Keycloak"-sectie aan KeycloakOptions
        services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Vult OpenIdConnectOptions vanuit KeycloakOptions via IConfigureNamedOptions<T>
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureKeycloakOptions>();

        // Cookie + OpenID Connect
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.HttpOnly = true;
                // Op HTTP (geen HTTPS) moet SameSite op None of Lax staan zodat
                // de browser de cookie meestuurt na redirect terug van Keycloak.
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            })
            .AddOpenIdConnect(options =>
            {
                // De correlation- en nonce-cookies die tijdens de OIDC-flow worden
                // aangemaakt moeten terugkomen na redirect van Keycloak. Op HTTP
                // is SecurePolicy.None vereist, anders weigert de browser ze mee te sturen.
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;
            }); // overige opties worden ingevuld door ConfigureKeycloakOptions

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddCascadingAuthenticationState();

        return services;
    }
}