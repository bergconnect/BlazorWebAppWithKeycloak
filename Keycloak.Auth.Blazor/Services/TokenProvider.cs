using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Keycloak.Auth.Blazor.Services;

/// <summary>
/// Houdt de tokens van de ingelogde gebruiker bij per Blazor circuit.
///
/// Gevuld door <see cref="BearerTokenHandler"/> tijdens de pre-render
/// HTTP-request via <see cref="LaadVanuitHttpContextAsync"/>.
/// Na een succesvolle refresh bijgewerkt via <see cref="SlaTokensOp"/>.
/// </summary>
public sealed class TokenProvider
{
    public string? AccessToken  { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? ExpiresAt    { get; private set; }

    /// <summary>
    /// True zodra tokens succesvol geladen zijn vanuit een HttpContext.
    /// Blijft false als HttpContext null was — zodat een volgende poging
    /// alsnog de tokens kan laden wanneer HttpContext wel beschikbaar is.
    /// </summary>
    public bool IsGeladen => AccessToken is not null || RefreshToken is not null;

    /// <summary>
    /// Laadt tokens uit de authenticatiecookie.
    /// Slaat over als al geladen, of als HttpContext geen tokens heeft.
    /// </summary>
    public async Task LaadVanuitHttpContextAsync(HttpContext httpContext)
    {
        if (IsGeladen) return;

        var accessToken  = await httpContext.GetTokenAsync("access_token");
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        var expiresAt    = await httpContext.GetTokenAsync("expires_at");

        // Alleen opslaan als er daadwerkelijk tokens in de cookie zitten
        if (accessToken is null && refreshToken is null) return;

        AccessToken  = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt    = expiresAt;
    }

    /// <summary>Slaat vernieuwde tokens op na een succesvolle refresh bij Keycloak.</summary>
    public void SlaTokensOp(string accessToken, string? refreshToken, string expiresAt)
    {
        AccessToken = accessToken;
        ExpiresAt   = expiresAt;
        if (!string.IsNullOrEmpty(refreshToken))
            RefreshToken = refreshToken;
    }

    /// <summary>
    /// Wist alle tokens. Wordt aangeroepen als de Keycloak-sessie niet meer
    /// actief is (invalid_grant / Session not active).
    /// </summary>
    public void WisTokens()
    {
        AccessToken  = null;
        RefreshToken = null;
        ExpiresAt    = null;
    }

    public bool HeeftRefreshToken => !string.IsNullOrEmpty(RefreshToken);

    public bool IsTokenVerlopenOfBijna(int bufferSeconden = 30)
    {
        if (!IsGeladen)                                          return false;
        if (string.IsNullOrEmpty(ExpiresAt))                    return false;
        if (!DateTimeOffset.TryParse(ExpiresAt, out var expiry)) return false;
        return expiry < DateTimeOffset.UtcNow.AddSeconds(bufferSeconden);
    }
}
