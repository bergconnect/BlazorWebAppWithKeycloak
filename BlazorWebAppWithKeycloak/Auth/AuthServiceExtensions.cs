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

        return services;
    }
}