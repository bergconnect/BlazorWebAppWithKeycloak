using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// Beheert het vernieuwen van access tokens via het refresh token.
/// Gebruik deze service in plaats van direct tokens op te halen,
/// zodat verlopen access tokens automatisch worden ververst.
/// </summary>
public sealed class TokenRefreshService(
    IHttpContextAccessor httpContextAccessor,
    IOptionsSnapshot<OpenIdConnectOptions> oidcOptions,
    ILogger<TokenRefreshService> logger)
{
    // Ververs het token alvast X seconden vóór de daadwerkelijke vervaldatum,
    // om race conditions bij gelijktijdige requests te voorkomen.
    private const int RefreshBufferSeconds = 30;

    /// <summary>
    /// Geeft een geldig access token terug.
    /// Als het token verlopen is (of bijna verloopt), wordt automatisch
    /// een nieuw token opgehaald via het refresh token.
    /// </summary>
    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        var expiresAt = await httpContext.GetTokenAsync("expires_at");

        if (IsTokenExpiredOrExpiringSoon(expiresAt))
        {
            logger.LogDebug("Access token verloopt binnenkort of is verlopen. Token wordt ververst.");
            return await RefreshAccessTokenAsync(httpContext, cancellationToken);
        }

        return await httpContext.GetTokenAsync("access_token");
    }

    private static bool IsTokenExpiredOrExpiringSoon(string? expiresAt)
    {
        if (string.IsNullOrEmpty(expiresAt))
            return true;

        if (!DateTimeOffset.TryParse(expiresAt, out var expiry))
            return true;

        return expiry < DateTimeOffset.UtcNow.AddSeconds(RefreshBufferSeconds);
    }

    private async Task<string?> RefreshAccessTokenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("Geen refresh token beschikbaar. Gebruiker moet opnieuw inloggen.");
            return null;
        }

        try
        {
            var options = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
            var configuration = await options.ConfigurationManager!
                .GetConfigurationAsync(cancellationToken);

            // Stuur token refresh request naar Keycloak
            var tokenEndpoint = configuration.TokenEndpoint;
            using var httpClient = new HttpClient();

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = options.ClientId!,
                ["client_secret"] = options.ClientSecret!,
                ["refresh_token"] = refreshToken,
            };

            var response = await httpClient.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(tokenRequest),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Token refresh mislukt met statuscode {StatusCode}. " +
                    "Gebruiker moet opnieuw inloggen.",
                    response.StatusCode);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                logger.LogWarning("Lege token response ontvangen van Keycloak.");
                return null;
            }

            // Sla de nieuwe tokens op in de authenticatiecookie
            await PersistRefreshedTokensAsync(httpContext, tokenResponse);

            logger.LogDebug("Access token succesvol ververst.");
            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Onverwachte fout bij token refresh.");
            return null;
        }
    }

    private static async Task PersistRefreshedTokensAsync(
        HttpContext httpContext,
        TokenResponse tokenResponse)
    {
        var authenticateResult = await httpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (authenticateResult?.Principal is null)
            return;

        var expiresAt = DateTimeOffset.UtcNow
            .AddSeconds(tokenResponse.ExpiresIn)
            .ToString("o"); // ISO 8601, zelfde formaat als OIDC middleware

        // Vervang de tokens in de bestaande authentication properties
        var newTokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token",  Value = tokenResponse.AccessToken },
            new() { Name = "expires_at",    Value = expiresAt },
        };

        // Behoud het refresh token (Keycloak geeft niet altijd een nieuw refresh token terug)
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            newTokens.Add(new() { Name = "refresh_token", Value = tokenResponse.RefreshToken });

        var authProperties = authenticateResult.Properties!;
        authProperties.StoreTokens(newTokens);

        // Schrijf de bijgewerkte cookie terug
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authenticateResult.Principal,
            authProperties);
    }

    /// <summary>
    /// Geeft terug of de huidige gebruiker een geldig refresh token heeft.
    /// Gebruik dit om te bepalen of een stille re-authenticatie mogelijk is
    /// of dat de gebruiker naar /login gestuurd moet worden.
    /// </summary>
    public async Task<bool> HasValidRefreshTokenAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return false;

        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        return !string.IsNullOrEmpty(refreshToken);
    }
}

/// <summary>
/// Minimale mapping van de Keycloak token response JSON.
/// </summary>
internal sealed class TokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
