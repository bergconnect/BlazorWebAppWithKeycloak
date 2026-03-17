using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Keycloak.Auth.Blazor.Internal;

/// <summary>
/// Configureert <see cref="OpenIdConnectOptions"/> met waarden uit <see cref="KeycloakOptions"/>
/// via de Options-pattern, zonder BuildServiceProvider() aan te roepen.
/// </summary>
internal sealed class ConfigureKeycloakOptions(IOptions<KeycloakOptions> keycloakOptions)
    : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly KeycloakOptions _keycloak = keycloakOptions.Value;

    // ASP.NET Core mapt Keycloak-rollen naar dit claim type
    private const string RoleClaimType =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

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

        // ASP.NET Core 9+ stuurt standaard PAR-requests. Keycloak vereist
        // expliciete activering van PAR per client. Schakel PAR hier uit
        // totdat dit in Keycloak is geconfigureerd.
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

        // Sla refresh_expires_in op als extra token zodat consumers
        // de vervaldatum van het refresh token kunnen tonen.
        options.Events = new OpenIdConnectEvents
        {
            OnTokenResponseReceived = ctx =>
            {
                var refreshExpiresIn = ctx.TokenEndpointResponse
                    .GetParameter("refresh_expires_in")?.ToString();

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
                                Name = "refresh_expires_at",
                                Value = refreshExpiresAt
                            }));
                }

                return Task.CompletedTask;
            }
        };
    }

    public void Configure(OpenIdConnectOptions options)
        => Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
}
