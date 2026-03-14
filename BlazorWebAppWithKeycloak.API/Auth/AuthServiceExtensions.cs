using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace BlazorWebAppWithKeycloak.API.Auth;

/// <summary>
/// Extension methods op <see cref="IServiceCollection"/> voor de
/// Keycloak JWT-authenticatie registratie.
/// </summary>
public static class AuthServiceExtensions
{
    private const string RoleClaimType =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    /// <summary>
    /// Registreert Keycloak JWT Bearer-authenticatie voor de API.
    /// Tokens worden gevalideerd op issuer, audience en handtekening via JWKS.
    /// </summary>
    public static IServiceCollection AddKeycloakJwtAuthentication(
        this IServiceCollection services)
    {
        services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Opties worden ingevuld via IConfigureOptions zodat we
                // geen BuildServiceProvider() hoeven aan te roepen.
            });

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthorization(options =>
        {
            // Policy die vereist dat de gebruiker de client-rol "user" heeft
            options.AddPolicy("UserRole", policy =>
                policy.RequireRole("user"));
        });

        return services;
    }
}
