using Microsoft.AspNetCore.Authentication;

namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// DelegatingHandler die het access token van de ingelogde gebruiker
/// toevoegt als Authorization Bearer header aan uitgaande HTTP-verzoeken.
/// Het token wordt automatisch ververst als het verlopen is of bijna verloopt,
/// via <see cref="TokenRefreshService"/>.
/// </summary>
public sealed class BearerTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    TokenRefreshService tokenRefreshService)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            // Gebruik TokenRefreshService: haalt automatisch een nieuw token op
            // als het huidige verlopen is of binnen de buffer-periode verloopt.
            var accessToken = await tokenRefreshService.GetValidAccessTokenAsync(cancellationToken);

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
            else
            {
                // Geen geldig token beschikbaar: sessie is verlopen en refresh is mislukt.
                // Geef 401 terug zodat de UI de gebruiker naar /login kan sturen.
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
