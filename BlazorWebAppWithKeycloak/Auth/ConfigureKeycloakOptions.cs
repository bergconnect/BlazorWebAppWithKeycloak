using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BlazorWebAppWithKeycloak.Auth;

/// <summary>
/// Configureert <see cref="OpenIdConnectOptions"/> met waarden uit <see cref="KeycloakOptions"/>
/// via de Options-pattern, zonder BuildServiceProvider() aan te roepen.
/// </summary>
public sealed class ConfigureKeycloakOptions
    : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly KeycloakOptions _keycloak;

    public ConfigureKeycloakOptions(IOptions<KeycloakOptions> keycloakOptions)
    {
        _keycloak = keycloakOptions.Value;
    }

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (name != OpenIdConnectDefaults.AuthenticationScheme)
            return;

        options.Authority = _keycloak.Authority;
        options.ClientId = _keycloak.ClientId;
        options.ClientSecret = _keycloak.ClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Keycloak stuurt rollen in "realm_access" claim
        options.ClaimActions.MapJsonKey("roles", "realm_access");

        options.RequireHttpsMetadata = _keycloak.RequireHttpsMetadata;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "roles";
    }

    public void Configure(OpenIdConnectOptions options)
        => Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
}