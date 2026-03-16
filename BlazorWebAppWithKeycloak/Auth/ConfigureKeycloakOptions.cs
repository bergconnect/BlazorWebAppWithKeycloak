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

    // ASP.NET Core mapt Keycloak-rollen naar dit claim type
    private const string RoleClaimType =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

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

        if (!string.IsNullOrEmpty(_keycloak.MetadataAddress))
            options.MetadataAddress = _keycloak.MetadataAddress;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.RequireHttpsMetadata = _keycloak.RequireHttpsMetadata;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = RoleClaimType;

        // Sla refresh_expires_in op als extra token zodat de Claims-pagina
        // de vervaldatum van het refresh token kan tonen.
        // refresh_expires_in zit als raw parameter in de token response body.
        options.Events = new OpenIdConnectEvents
        {
            OnTokenResponseReceived = ctx =>
            {
                // Keycloak geeft refresh_expires_in terug als parameter in de JSON response
                var refreshExpiresIn = ctx.TokenEndpointResponse.GetParameter("refresh_expires_in")?.ToString();
                if (!string.IsNullOrEmpty(refreshExpiresIn)
                    && int.TryParse(refreshExpiresIn, out var seconds))
                {
                    var refreshExpiresAt = DateTimeOffset.UtcNow
                        .AddSeconds(seconds)
                        .ToString("o");

                    ctx.Properties!.StoreTokens(
                        ctx.Properties.GetTokens().Append(
                            new AuthenticationToken
                            {
                                Name  = "refresh_expires_at",
                                Value = refreshExpiresAt
                            }));
                }

                return Task.CompletedTask;
            }
        };

        // ASP.NET Core 9+ stuurt standaard PAR-requests. Keycloak vereist
        // expliciete activering van PAR per client (Clients → Advanced →
        // "Pushed authorization request required"). Schakel PAR hier uit
        // totdat dit in Keycloak is geconfigureerd.
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    }

    public void Configure(OpenIdConnectOptions options)
        => Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
}