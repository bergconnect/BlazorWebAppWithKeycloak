using System.ComponentModel.DataAnnotations;

namespace BlazorWebAppWithKeycloak.API.Auth;

/// <summary>
/// Sterk-getypeerde configuratie voor de Keycloak JWT-validatie.
/// Gebonden aan de sectie "Keycloak" in appsettings.json via de Options-pattern.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Gedeeld role claim type — consistent met de Blazor Web App instelling.
    /// ASP.NET Core mapt Keycloak-rollen naar dit Microsoft schema-URI.
    /// </summary>
    public const string RoleClaimType =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    /// <summary>
    /// De publieke Authority URL, inclusief realm-pad.
    /// Wordt gebruikt voor JWT-issuer validatie en het ophalen van de JWKS.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Optionele interne metadata-URL voor server-to-server communicatie.
    /// Nodig in Docker wanneer de API Keycloak bereikt via een interne hostnaam.
    /// </summary>
    public string? MetadataAddress { get; init; }

    /// <summary>
    /// De Client ID waarvoor tokens gevalideerd worden (audience-check).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// HTTPS vereisen voor de metadata endpoint.
    /// Zet op <c>true</c> in productie.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = false;
}
