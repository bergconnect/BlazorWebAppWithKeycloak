using Microsoft.AspNetCore.Authentication;

namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// DelegatingHandler die het access token van de ingelogde gebruiker
/// toevoegt als Authorization Bearer header aan uitgaande HTTP-verzoeken.
/// </summary>
public sealed class BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
            else
            {
                // Geen token beschikbaar: verzoek niet doorsturen zonder authenticatie.
                // Dit voorkomt dat anonieme requests de API bereiken als
                // de sessie is verlopen maar de HttpContext nog beschikbaar is.
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
