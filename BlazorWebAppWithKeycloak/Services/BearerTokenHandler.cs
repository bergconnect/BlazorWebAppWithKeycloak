namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// DelegatingHandler die bij elke uitgaande HTTP-request:
/// <list type="number">
///   <item>Tokens laadt vanuit HttpContext als die beschikbaar is (pre-render fase)</item>
///   <item>Het access token valideert en vernieuwt via TokenService indien nodig</item>
///   <item>Het geldige token als Authorization Bearer header toevoegt</item>
/// </list>
/// </summary>
public sealed class BearerTokenHandler(
    IHttpContextAccessor        httpContextAccessor,
    TokenProvider               tokenProvider,
    TokenService                tokenService,
    ILogger<BearerTokenHandler> logger)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken  cancellationToken)
    {
        // Stap 1 — tokens laden vanuit HttpContext (alleen beschikbaar in pre-render fase)
        // IsGeladen is false zolang er nog geen tokens zijn — ook na een eerdere
        // mislukte poging waarbij HttpContext null was.
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null && !tokenProvider.IsGeladen)
            await tokenProvider.LaadVanuitHttpContextAsync(httpContext);

        // Stap 2 — geldig token ophalen; vernieuwt automatisch indien verlopen
        var accessToken = await tokenService.GetGeldigTokenAsync(cancellationToken);

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }

        // Tokens nog niet geladen (circuit-fase vóór eerste pre-render)
        // of sessie verlopen — geef 401 terug zodat de UI dit kan afhandelen
        logger.LogWarning(
            "Geen geldig access token voor {Method} {Uri}. IsGeladen={IsGeladen}.",
            request.Method, request.RequestUri, tokenProvider.IsGeladen);

        return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
    }
}
