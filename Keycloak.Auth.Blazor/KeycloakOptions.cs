using System.ComponentModel.DataAnnotations;

namespace Keycloak.Auth.Blazor;

/// <summary>
/// Sterk-getypeerde configuratie voor de Keycloak OIDC-verbinding.
/// Gebonden aan de sectie "Keycloak" in appsettings.json via de Options-pattern.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// De publieke OIDC Authority URL, inclusief realm-pad.
    /// Dit is de URL waarmee de browser communiceert.
    /// Voorbeeld: https://idp.example.nl/realms/mijn-realm
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Optionele interne metadata-URL voor server-to-server communicatie.
    /// Stel in wanneer de server Keycloak bereikt via een andere hostnaam dan de browser,
    /// bijvoorbeeld http://keycloak:8080/realms/mijn-realm/.well-known/openid-configuration
    /// in een Docker-omgeving.
    /// </summary>
    public string? MetadataAddress { get; init; }

    /// <summary>De Client ID zoals geconfigureerd in Keycloak.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Het Client Secret (alleen voor confidential clients).
    /// Stel in via omgevingsvariabele of user-secrets — nooit in appsettings.json.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>HTTPS vereisen voor de metadata endpoint. Zet op true in productie.</summary>
    public bool RequireHttpsMetadata { get; init; } = false;
}
