using Keycloak.Auth.Api.Internal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keycloak.Auth.Api;

/// <summary>
/// Extensiemethoden voor het registreren van Keycloak JWT-authenticatie
/// in een Minimal API applicatie.
///
/// Gebruik:
/// <code>
/// // Program.cs
/// builder.Services.AddKeycloakApiAuth();
/// builder.Services.AddKeycloakApiAuth(options =>
/// {
///     options.AddPolicy("AdminRole", p => p.RequireRole("admin"));
/// });
/// </code>
/// </summary>
public static class KeycloakAuthApiExtensions
{
    /// <summary>
    /// Registreert Keycloak JWT Bearer-authenticatie met de standaard UserRole policy.
    /// Leest configuratie uit de sectie "Keycloak" in appsettings.json.
    /// </summary>
    public static IServiceCollection AddKeycloakApiAuth(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configureAuthorization = null)
    {
        services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { });

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthorization(options =>
        {
            // Standaard policy — vereist de 'user' client-rol
            options.AddPolicy("UserRole", policy => policy.RequireRole("user"));

            // Optionele extra policies via de caller
            configureAuthorization?.Invoke(options);
        });

        return services;
    }
}