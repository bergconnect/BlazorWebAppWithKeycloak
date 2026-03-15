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
                // Op HTTPS kan de Secure-flag veilig worden ingesteld.
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(options =>
            {
                // Op HTTPS werkt SameSite=Lax correct voor cross-site redirects.
                // De Secure-flag zorgt dat cookies alleen over HTTPS worden verstuurd.
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddCascadingAuthenticationState();

        return services;
    }
}