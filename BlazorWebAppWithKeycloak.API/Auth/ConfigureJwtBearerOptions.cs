using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BlazorWebAppWithKeycloak.API.Auth;

/// <summary>
/// Configureert <see cref="JwtBearerOptions"/> met waarden uit <see cref="KeycloakOptions"/>
/// via de Options-pattern, zonder BuildServiceProvider() aan te roepen.
/// </summary>
public sealed class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private const string RoleClaimType =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private readonly KeycloakOptions _keycloak;

    public ConfigureJwtBearerOptions(IOptions<KeycloakOptions> keycloakOptions)
    {
        _keycloak = keycloakOptions.Value;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        options.Authority = _keycloak.Authority;

        if (!string.IsNullOrEmpty(_keycloak.MetadataAddress))
            options.MetadataAddress = _keycloak.MetadataAddress;

        options.RequireHttpsMetadata = _keycloak.RequireHttpsMetadata;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = RoleClaimType,

            // Accepteer zowel "blazor-web-app" als "account" als geldige audience.
            // "account" is de Keycloak standaard totdat de audience-mapper actief is.
            // Zodra de mapper is geconfigureerd en het token "blazor-web-app" bevat,
            // kan ValidAudiences worden teruggebracht tot alleen "blazor-web-app".
            ValidateAudience = true,
            ValidAudiences = [_keycloak.ClientId, "account"],

            ValidIssuers = string.IsNullOrEmpty(_keycloak.MetadataAddress)
                ? null
                : [_keycloak.Authority, _keycloak.Authority.TrimEnd('/')],
        };
    }

    public void Configure(JwtBearerOptions options)
        => Configure(JwtBearerDefaults.AuthenticationScheme, options);
}