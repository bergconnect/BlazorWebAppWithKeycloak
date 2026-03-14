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
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            })
            .AddOpenIdConnect(options =>
            {
                // Keycloak draait op een andere host dan de app (cross-site redirect).
                // SameSite=Unspecified stuurt geen SameSite-attribuut mee waardoor
                // oudere browsers en HTTP-omgevingen de cookie altijd doorsturen.
                // SecurePolicy.None zorgt dat de Secure-flag ontbreekt op HTTP.
                options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
                options.NonceCookie.SameSite = SameSiteMode.Unspecified;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;
            }); // overige opties worden ingevuld door ConfigureKeycloakOptions

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddCascadingAuthenticationState();

        return services;
    }
}