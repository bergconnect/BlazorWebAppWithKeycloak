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

        // Keycloak stuurt rollen als array in "realm_access.roles"
        // MapJsonSubKey mapt het geneste pad correct naar de "roles" claim
        options.ClaimActions.MapJsonSubKey("roles", "realm_access", "roles");

        options.RequireHttpsMetadata = _keycloak.RequireHttpsMetadata;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "roles";

        // ASP.NET Core 9+ stuurt standaard PAR-requests. Keycloak vereist
        // expliciete activering van PAR per client (Clients → Advanced →
        // "Pushed authorization request required"). Schakel PAR hier uit
        // totdat dit in Keycloak is geconfigureerd.
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    }

    public void Configure(OpenIdConnectOptions options)
        => Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
}