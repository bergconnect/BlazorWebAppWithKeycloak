using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// Voert de token refresh uit bij Keycloak via de refresh token grant.
/// Geregistreerd als <c>Scoped</c> — gebruikt <see cref="TokenProvider"/>
/// voor het lezen en opslaan van tokens.
/// </summary>
public sealed class TokenService(
    TokenProvider tokenProvider,
    IOptionsSnapshot<OpenIdConnectOptions> oidcOptions,
    ILogger<TokenService> logger)
{
    /// <summary>
    /// Valideert het huidige access token en vernieuwt het indien nodig.
    /// Geeft het geldige access token terug, of <c>null</c> als refresh mislukt.
    /// </summary>
    public async Task<string?> GetGeldigTokenAsync(CancellationToken ct = default)
    {
        // Token is nog geldig — direct teruggeven
        if (!tokenProvider.IsTokenVerlopenOfBijna())
            return tokenProvider.AccessToken;

        // Token verlopen of bijna verlopen — probeer te verversen
        return await VervangTokenAsync(ct);
    }

    private async Task<string?> VervangTokenAsync(CancellationToken ct)
    {
        if (!tokenProvider.HeeftRefreshToken)
        {
            logger.LogWarning(
                "Geen refresh token beschikbaar — gebruiker moet opnieuw inloggen.");
            return null;
        }

        try
        {
            var options = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
            var config = await options.ConfigurationManager!.GetConfigurationAsync(ct);

            logger.LogDebug("Access token verversen via {TokenEndpoint}.", config.TokenEndpoint);

            using var http = new HttpClient();
            var body = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = options.ClientId!,
                ["client_secret"] = options.ClientSecret!,
                ["refresh_token"] = tokenProvider.RefreshToken!,
            };

            var response = await http.PostAsync(
                config.TokenEndpoint,
                new FormUrlEncodedContent(body),
                ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Token refresh mislukt — Keycloak antwoordde met {StatusCode}.",
                    (int)response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(ct);
            if (result is null || string.IsNullOrEmpty(result.AccessToken))
            {
                logger.LogWarning("Token refresh: lege response van Keycloak.");
                return null;
            }

            var expiresAt = DateTimeOffset.UtcNow
                .AddSeconds(result.ExpiresIn)
                .ToString("o");

            tokenProvider.SlaTokensOp(result.AccessToken, result.RefreshToken, expiresAt);

            logger.LogInformation("Access token succesvol ververst.");
            return result.AccessToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Onverwachte fout tijdens token refresh.");
            return null;
        }
    }
}

internal sealed class KeycloakTokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
