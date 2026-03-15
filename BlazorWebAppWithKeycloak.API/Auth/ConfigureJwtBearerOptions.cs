using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BlazorWebAppWithKeycloak.API.Auth;

/// <summary>
/// Configureert <see cref="JwtBearerOptions"/> met waarden uit <see cref="KeycloakOptions"/>
/// via de Options-pattern, zonder BuildServiceProvider() aan te roepen.
/// </summary>
public sealed class ConfigureJwtBearerOptions(IOptions<KeycloakOptions> keycloakOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly KeycloakOptions _keycloak = keycloakOptions.Value;

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
            ValidateAudience = true,
            ValidateLifetime = true,

            // Sta geen klokafwijking toe groter dan 30 seconden.
            // Standaard is dit 5 minuten, wat een aanvaller ruimte geeft
            // om verlopen tokens te hergebruiken.
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = "preferred_username",
            RoleClaimType = KeycloakOptions.RoleClaimType,

            ValidAudiences = [_keycloak.ClientId, "account"],

            ValidIssuers = string.IsNullOrEmpty(_keycloak.MetadataAddress)
                ? null
                : [_keycloak.Authority, _keycloak.Authority.TrimEnd('/')],
        };
    }

    public void Configure(JwtBearerOptions options)
        => Configure(JwtBearerDefaults.AuthenticationScheme, options);
}
