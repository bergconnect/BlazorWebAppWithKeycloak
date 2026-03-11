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
            })
            .AddOpenIdConnect(); // opties worden ingevuld door ConfigureKeycloakOptions

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddCascadingAuthenticationState();

        return services;
    }
}