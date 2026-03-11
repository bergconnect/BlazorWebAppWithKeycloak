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
    /// De OIDC Authority URL, inclusief realm-pad.
    /// Voorbeeld: http://localhost:8080/realms/blazor
    /// </summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// De Client ID zoals geconfigureerd in Keycloak.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Het Client Secret (alleen voor confidential clients).
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// HTTPS vereisen voor de metadata endpoint.
    /// Zet op <c>true</c> in productie.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = false;
}
