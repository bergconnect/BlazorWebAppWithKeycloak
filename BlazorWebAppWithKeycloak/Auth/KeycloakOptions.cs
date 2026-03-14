using System.ComponentModel.DataAnnotations;

namespace BlazorWebAppWithKeycloak.Auth;

/// <summary>
/// Sterk-getypeerde configuratie voor de Keycloak OIDC-verbinding.
/// Gebonden aan de sectie "Keycloak" in appsettings.json via de Options-pattern.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>Naam van de sectie in appsettings.json.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// De publieke OIDC Authority URL, inclusief realm-pad.
    /// Dit is de URL waarmee de browser communiceert (bv. http://localhost:8082/realms/homelab).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Optionele interne metadata-URL voor server-to-server communicatie.
    /// Stel in wanneer de server Keycloak bereikt via een andere hostnaam dan de browser,
    /// bv. http://keycloak:8082/realms/homelab/.well-known/openid-configuration in Docker.
    /// </summary>
    public string? MetadataAddress { get; init; }

    /// <summary>
    /// De Client ID zoals geconfigureerd in Keycloak.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Het Client Secret (alleen voor confidential clients).
    /// Stel in via omgevingsvariabele of user-secrets, niet via appsettings.json.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// HTTPS vereisen voor de metadata endpoint.
    /// Zet op <c>true</c> in productie.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = false;
}